namespace UrlShortener.Application.Authentication;

public sealed record IssuedAccessToken(string Value, DateTime ExpiresAtUtc);
