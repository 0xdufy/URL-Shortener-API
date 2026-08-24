using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using UrlShortener.Infrastructure.Configuration;

namespace UrlShortener.Infrastructure.Messaging;

public sealed class RabbitMqTopologyInitializer
{
    private readonly RabbitMqConnectionProvider _connectionProvider;
    private readonly RabbitMqOptions _options;
    private readonly TimeSpan _operationTimeout;

    public RabbitMqTopologyInitializer(
        RabbitMqConnectionProvider connectionProvider,
        IOptions<RabbitMqOptions> options)
    {
        _connectionProvider = connectionProvider;
        _options = options.Value;
        _operationTimeout = TimeSpan.FromMilliseconds(_options.OperationTimeoutMilliseconds);
    }

    public async Task EnsureTopologyAsync(CancellationToken cancellationToken = default)
    {
        var connection = await _connectionProvider.GetConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await DeclareAsync(channel, cancellationToken);
    }

    internal async Task DeclareAsync(IChannel channel, CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(_operationTimeout);
        var operationToken = timeoutSource.Token;
        var messageRetentionMilliseconds = checked((long)TimeSpan
            .FromDays(_options.MessageRetentionDays)
            .TotalMilliseconds);

        await channel.ExchangeDeclareAsync(
            _options.ExchangeName,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: operationToken);

        await channel.ExchangeDeclareAsync(
            _options.DeadLetterExchangeName,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: operationToken);

        var deadLetterQueueArguments = new Dictionary<string, object?>
        {
            ["x-queue-type"] = "quorum",
            ["x-message-ttl"] = messageRetentionMilliseconds
        };
        await channel.QueueDeclareAsync(
            _options.DeadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: deadLetterQueueArguments,
            cancellationToken: operationToken);
        await channel.QueueBindAsync(
            _options.DeadLetterQueueName,
            _options.DeadLetterExchangeName,
            _options.DeadLetterRoutingKey,
            cancellationToken: operationToken);

        var queueArguments = new Dictionary<string, object?>
        {
            ["x-queue-type"] = "quorum",
            ["x-message-ttl"] = messageRetentionMilliseconds,
            ["x-delivery-limit"] = _options.DeliveryLimit,
            ["x-dead-letter-exchange"] = _options.DeadLetterExchangeName,
            ["x-dead-letter-routing-key"] = _options.DeadLetterRoutingKey,
            ["x-dead-letter-strategy"] = "at-least-once",
            ["x-overflow"] = "reject-publish"
        };
        await channel.QueueDeclareAsync(
            _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArguments,
            cancellationToken: operationToken);
        await channel.QueueBindAsync(
            _options.QueueName,
            _options.ExchangeName,
            _options.RoutingKey,
            cancellationToken: operationToken);
    }
}
