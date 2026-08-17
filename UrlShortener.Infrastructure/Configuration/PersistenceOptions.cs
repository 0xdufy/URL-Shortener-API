namespace UrlShortener.Infrastructure.Configuration;

public sealed class PersistenceOptions
{
    public const string SectionName = "Persistence";

    public int CommandTimeoutSeconds { get; set; } = 30;
}
