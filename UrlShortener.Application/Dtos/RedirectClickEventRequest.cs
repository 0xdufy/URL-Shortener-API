namespace UrlShortener.Application.Dtos;

public sealed record RedirectClickEventRequest(
    Guid ShortUrlId,
    DateTimeOffset AccessedAtUtc,
    string ClientIpAddress,
    string? UserAgent,
    string? Referer);
