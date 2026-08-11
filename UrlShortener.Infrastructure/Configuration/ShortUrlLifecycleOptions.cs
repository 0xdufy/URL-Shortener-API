namespace UrlShortener.Infrastructure.Configuration;

public sealed class ShortUrlLifecycleOptions
{
    public const string SectionName = "ShortUrlLifecycle";

    public int SoftDeleteRetentionDays { get; set; } = 30;
}
