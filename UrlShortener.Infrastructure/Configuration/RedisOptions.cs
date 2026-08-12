namespace UrlShortener.Infrastructure.Configuration;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public int ConnectTimeoutMilliseconds { get; set; } = 2_000;
    public int OperationTimeoutMilliseconds { get; set; } = 1_000;
    public int ConnectRetryCount { get; set; } = 2;
    public int ReconnectBaseDelayMilliseconds { get; set; } = 1_000;
    public int ReconnectMaxDelayMilliseconds { get; set; } = 5_000;
}
