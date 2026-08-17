using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using UrlShortener.Application.Interfaces;
using UrlShortener.Application.Messaging;
using UrlShortener.Infrastructure.Configuration;

namespace UrlShortener.Infrastructure.Messaging;

public sealed class RabbitMqEventPublisher : IEventPublisher, IAsyncDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly RabbitMqConnectionProvider _connectionProvider;
    private readonly RabbitMqTopologyInitializer _topologyInitializer;
    private readonly RabbitMqOptions _options;
    private readonly TimeSpan _operationTimeout;
    private readonly SemaphoreSlim _publishLock = new(1, 1);
    private IChannel? _channel;
    private bool _disposed;

    public RabbitMqEventPublisher(
        RabbitMqConnectionProvider connectionProvider,
        RabbitMqTopologyInitializer topologyInitializer,
        IOptions<RabbitMqOptions> options)
    {
        _connectionProvider = connectionProvider;
        _topologyInitializer = topologyInitializer;
        _options = options.Value;
        _operationTimeout = TimeSpan.FromMilliseconds(_options.OperationTimeoutMilliseconds);
    }

    public async Task PublishAsync<TPayload>(
        IntegrationEvent<TPayload> integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        Validate(integrationEvent);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var body = JsonSerializer.SerializeToUtf8Bytes(integrationEvent, SerializerOptions);
        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            ContentEncoding = "utf-8",
            MessageId = integrationEvent.EventId.ToString("D"),
            Type = integrationEvent.EventName,
            Timestamp = new AmqpTimestamp(integrationEvent.OccurredAtUtc.ToUnixTimeSeconds()),
            Headers = new Dictionary<string, object?>
            {
                ["contract-version"] = integrationEvent.ContractVersion
            }
        };

        await _publishLock.WaitAsync(cancellationToken);
        try
        {
            var channel = await GetChannelAsync(cancellationToken);
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(_operationTimeout);
            await channel.BasicPublishAsync(
                _options.ExchangeName,
                _options.RoutingKey,
                mandatory: true,
                basicProperties: properties,
                body,
                timeoutSource.Token);
        }
        finally
        {
            _publishLock.Release();
        }
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }

        var connection = await _connectionProvider.GetConnectionAsync(cancellationToken);
        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);
        _channel = await connection.CreateChannelAsync(channelOptions, cancellationToken);
        await _topologyInitializer.DeclareAsync(_channel, cancellationToken);
        return _channel;
    }

    private static void Validate<TPayload>(IntegrationEvent<TPayload> integrationEvent)
    {
        if (integrationEvent.EventId == Guid.Empty)
        {
            throw new ArgumentException("The event ID cannot be empty.", nameof(integrationEvent));
        }

        if (string.IsNullOrWhiteSpace(integrationEvent.EventName) || integrationEvent.EventName.Length > 200)
        {
            throw new ArgumentException("The event name must contain 1 to 200 characters.", nameof(integrationEvent));
        }

        if (integrationEvent.ContractVersion <= 0)
        {
            throw new ArgumentException("The event contract version must be positive.", nameof(integrationEvent));
        }

        if (integrationEvent.OccurredAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("The event occurrence timestamp must use the UTC offset.", nameof(integrationEvent));
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await _publishLock.WaitAsync();
        try
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_channel is not null)
            {
                await _channel.DisposeAsync();
                _channel = null;
            }
        }
        finally
        {
            _publishLock.Release();
            _publishLock.Dispose();
        }
    }
}
