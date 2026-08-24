namespace UrlShortener.Application.Dtos;

public sealed record RedirectLookupModel(
    Guid ShortUrlId,
    string RoutingHost,
    string ShortCode,
    string OriginalUrl,
    DateTime? ExpiresAtUtc,
    bool IsActive,
    bool IsDeleted,
    bool IsModerationBlocked,
    bool IsOwnerUnavailable);
