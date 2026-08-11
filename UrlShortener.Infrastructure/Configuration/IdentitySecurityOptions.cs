namespace UrlShortener.Infrastructure.Configuration;

public sealed class IdentitySecurityOptions
{
    public const string SectionName = "Identity";

    public bool PublicRegistrationEnabled { get; init; } = true;
    public bool RequireSecureCookies { get; init; } = true;
    public string JwtIssuer { get; init; } = "UrlShortener.Api";
    public string JwtAudience { get; init; } = "UrlShortener.Client";
    public string JwtSigningKeyBase64 { get; init; } = string.Empty;
    public int JwtClockSkewSeconds { get; init; } = 30;
    public string[] AllowedOrigins { get; init; } = [];
    public int PasswordRequiredLength { get; init; } = 12;
    public int PasswordRequiredUniqueChars { get; init; } = 4;
    public int MaxFailedAccessAttempts { get; init; } = 5;
    public int LockoutMinutes { get; init; } = 15;
    public int AccessTokenLifetimeMinutes { get; init; } = 10;
    public int RefreshTokenLifetimeDays { get; init; } = 30;
    public int RefreshTokenAbsoluteLifetimeDays { get; init; } = 90;
}
