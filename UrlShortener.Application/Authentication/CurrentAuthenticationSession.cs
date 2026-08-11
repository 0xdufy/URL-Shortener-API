namespace UrlShortener.Application.Authentication;

public sealed record CurrentAuthenticationSession(
    Guid SessionId,
    DateTime RefreshSessionCreatedAtUtc,
    DateTime RefreshSessionExpiresAtUtc,
    bool IsRefreshSessionRevoked,
    AuthenticatedUser User);
