namespace UrlShortener.Application.Dtos;

public sealed class ShortUrlCacheModel
{
    public const int CurrentSchemaVersion = 2;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public Guid ShortUrlId { get; init; }
    public string RoutingHost { get; init; } = string.Empty;
    public string OriginalUrl { get; init; } = string.Empty;
    public DateTime? ExpiresAtUtc { get; init; }
}
