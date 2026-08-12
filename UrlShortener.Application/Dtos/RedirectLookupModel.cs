namespace UrlShortener.Application.Dtos;

public sealed record RedirectLookupModel(
    Guid ShortUrlId,
    string ShortCode,
    string OriginalUrl,
    DateTime? ExpiresAtUtc,
    bool IsActive,
    bool IsDeleted);
