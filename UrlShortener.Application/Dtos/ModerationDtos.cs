namespace UrlShortener.Application.Dtos;

public sealed class ModerateShortUrlRequest
{
    public bool IsBlocked { get; set; }
    public string? PublicReasonCode { get; set; }
    public string InternalReason { get; set; } = string.Empty;
}

public sealed record ShortUrlModerationResponse(
    Guid ShortUrlId,
    string Status,
    string? PublicReasonCode,
    DateTime ModeratedAtUtc);
