namespace UrlShortener.Application.Dtos;

public sealed class CurrentAuthenticationSessionResponse
{
    public Guid SessionId { get; init; }
    public DateTime RefreshSessionCreatedAtUtc { get; init; }
    public DateTime RefreshSessionExpiresAtUtc { get; init; }
    public bool IsRefreshSessionRevoked { get; init; }
    public AuthenticatedUserResponse User { get; init; } = new();
}
