namespace UrlShortener.Application.Dtos;

public sealed class ShortUrlCacheModel
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public Guid ShortUrlId { get; init; }
    public string OriginalUrl { get; init; } = string.Empty;
    public DateTime? ExpiresAtUtc { get; init; }
}
