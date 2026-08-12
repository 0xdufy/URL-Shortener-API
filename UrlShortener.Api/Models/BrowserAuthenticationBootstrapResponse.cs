namespace UrlShortener.Api.Models;

public sealed class BrowserAuthenticationBootstrapResponse
{
    public string CsrfToken { get; init; } = string.Empty;
    public bool PublicRegistrationEnabled { get; init; }
    public int PasswordRequiredLength { get; init; }
    public int PasswordRequiredUniqueChars { get; init; }
}
