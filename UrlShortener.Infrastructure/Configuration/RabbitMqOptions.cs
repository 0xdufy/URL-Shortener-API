namespace UrlShortener.Infrastructure.Configuration;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool UseTls { get; set; }
    public string TlsServerName { get; set; } = string.Empty;
    public string ClientProvidedName { get; set; } = "url-shortener";
    public int ConnectionTimeoutMilliseconds { get; set; } = 5_000;
    public int OperationTimeoutMilliseconds { get; set; } = 5_000;
    public int RequestedHeartbeatSeconds { get; set; } = 30;
    public int NetworkRecoveryIntervalMilliseconds { get; set; } = 5_000;
    public ushort ConsumerPrefetchCount { get; set; } = 32;
    public int DeliveryLimit { get; set; } = 5;
    public int RetryBaseDelayMilliseconds { get; set; } = 250;
    public string ExchangeName { get; set; } = "url-shortener.events.v1";
    public string QueueName { get; set; } = "url-shortener.analytics.clicks.v1";
    public string RoutingKey { get; set; } = "analytics.click.v1";
    public string DeadLetterExchangeName { get; set; } = "url-shortener.events.dead.v1";
    public string DeadLetterQueueName { get; set; } = "url-shortener.analytics.clicks.dead.v1";
    public string DeadLetterRoutingKey { get; set; } = "analytics.click.v1.failed";
}
