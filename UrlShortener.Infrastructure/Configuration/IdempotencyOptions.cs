namespace UrlShortener.Infrastructure.Configuration;

public sealed class IdempotencyOptions
{
    public const string SectionName = "Idempotency";

    public int RetentionHours { get; set; } = 24;
}
