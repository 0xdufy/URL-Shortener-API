namespace UrlShortener.Application.Dtos;

public sealed record RedirectAccessRequest(
    Guid ShortUrlId,
    string OriginalUrl,
    DateTime? ExpiresAtUtc,
    DateTime AccessedAtUtc,
    string IpAddress,
    string? UserAgent,
    string? Referer);
