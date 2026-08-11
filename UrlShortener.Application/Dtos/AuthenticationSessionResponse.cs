namespace UrlShortener.Application.Dtos;

public sealed class AuthenticationSessionResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string TokenType { get; init; } = "Bearer";
    public DateTime AccessTokenExpiresAtUtc { get; init; }
    public DateTime RefreshSessionExpiresAtUtc { get; init; }
    public string CsrfToken { get; init; } = string.Empty;
    public AuthenticatedUserResponse User { get; init; } = new();
}
