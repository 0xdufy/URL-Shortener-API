namespace UrlShortener.Infrastructure.Configuration;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public int CreatePerMinuteLimit { get; set; } = 20;
}
