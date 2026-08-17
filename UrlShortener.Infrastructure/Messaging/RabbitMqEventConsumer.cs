using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using UrlShortener.Application.Interfaces;
using UrlShortener.Application.Messaging;
using UrlShortener.Infrastructure.Configuration;

namespace UrlShortener.Infrastructure.Messaging;

public sealed class RabbitMqEventConsumer : IEventConsumer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly RabbitMqConnectionProvider _connectionProvider;
    private readonly RabbitMqTopologyInitializer _topologyInitializer;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqEventConsumer> _logger;

    public RabbitMqEventConsumer(
        RabbitMqConnectionProvider connectionProvider,
        RabbitMqTopologyInitializer topologyInitializer,
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqEventConsumer> logger)
    {
        _connectionProvider = connectionProvider;
        _topologyInitializer = topologyInitializer;
        _options = options.Value;
        _logger = logger;
    }

    public async Task ConsumeAsync<TPayload>(
        string expectedEventName,
        int expectedContractVersion,
        Func<IntegrationEvent<TPayload>, CancellationToken, Task<EventHandlingOutcome>> handler,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedEventName);
        ArgumentNullException.ThrowIfNull(handler);
        if (expectedContractVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expectedContractVersion),
                "The expected contract version must be positive.");
        }

        var connection = await _connectionProvider.GetConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await _topologyInitializer.DeclareAsync(channel, cancellationToken);
        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: _options.ConsumerPrefetchCount,
            global: false,
            cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, delivery) =>
        {
            await ProcessDeliveryAsync(
                channel,
                delivery,
                expectedEventName,
                expectedContractVersion,
                handler,
                cancellationToken);
        };

        var consumerTag = await channel.BasicConsumeAsync(
            _options.QueueName,
            autoAck: false,
            consumer,
            cancellationToken);

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            if (channel.IsOpen)
            {
                await channel.BasicCancelAsync(consumerTag, noWait: false, CancellationToken.None);
            }
        }
    }

    private async Task ProcessDeliveryAsync<TPayload>(
        IChannel channel,
        BasicDeliverEventArgs delivery,
        string expectedEventName,
        int expectedContractVersion,
        Func<IntegrationEvent<TPayload>, CancellationToken, Task<EventHandlingOutcome>> handler,
        CancellationToken cancellationToken)
    {
        IntegrationEvent<TPayload>? integrationEvent;
        try
        {
            integrationEvent = JsonSerializer.Deserialize<IntegrationEvent<TPayload>>(
                delivery.Body.Span,
                SerializerOptions);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(
                exception,
                "Dead-lettering event delivery {DeliveryTag} because its JSON envelope is invalid.",
                delivery.DeliveryTag);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        if (integrationEvent is null ||
            integrationEvent.EventId == Guid.Empty ||
            !string.Equals(integrationEvent.EventName, expectedEventName, StringComparison.Ordinal) ||
            integrationEvent.ContractVersion != expectedContractVersion)
        {
            _logger.LogWarning(
                "Dead-lettering event delivery {DeliveryTag} because its envelope metadata does not match {EventName} version {ContractVersion}.",
                delivery.DeliveryTag,
                expectedEventName,
                expectedContractVersion);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        EventHandlingOutcome outcome;
        try
        {
            outcome = await handler(integrationEvent, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: true);
            return;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Event handler failed for event {EventId}; the delivery will be retried within the configured limit.",
                integrationEvent.EventId);
            outcome = EventHandlingOutcome.Retry;
        }

        switch (outcome)
        {
            case EventHandlingOutcome.Completed:
                await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false);
                break;
            case EventHandlingOutcome.Retry:
                await DelayBeforeRetryAsync(delivery, cancellationToken);
                await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: true);
                break;
            case EventHandlingOutcome.DeadLetter:
                await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false);
                break;
            default:
                throw new InvalidOperationException($"Unsupported event handling outcome '{outcome}'.");
        }
    }

    private async Task DelayBeforeRetryAsync(
        BasicDeliverEventArgs delivery,
        CancellationToken cancellationToken)
    {
        var deliveryCount = GetDeliveryCount(delivery.BasicProperties.Headers);
        var exponent = Math.Min(deliveryCount, 5);
        var delayMilliseconds = Math.Min(
            _options.RetryBaseDelayMilliseconds * (1 << exponent),
            30_000);
        await Task.Delay(delayMilliseconds, cancellationToken);
    }

    private static int GetDeliveryCount(IDictionary<string, object?>? headers)
    {
        if (headers is null || !headers.TryGetValue("x-delivery-count", out var value))
        {
            return 0;
        }

        return value switch
        {
            byte byteValue => byteValue,
            short shortValue => shortValue,
            int intValue => intValue,
            long longValue when longValue <= int.MaxValue => (int)longValue,
            _ => 0
        };
    }
}
