namespace UrlShortener.Infrastructure.Configuration;

public sealed class PersistenceOptions
{
    public const string SectionName = "Persistence";

    public int MaxRetryCount { get; set; } = 3;
    public int MaxRetryDelaySeconds { get; set; } = 5;
    public int CommandTimeoutSeconds { get; set; } = 30;
}
