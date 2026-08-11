namespace UrlShortener.Application.Authentication;

public sealed record IssuedAuthenticationSession(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshSessionExpiresAtUtc,
    AuthenticatedUser User);
