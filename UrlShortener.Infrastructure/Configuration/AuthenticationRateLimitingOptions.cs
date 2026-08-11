namespace UrlShortener.Infrastructure.Configuration;

public sealed class AuthenticationRateLimitingOptions
{
    public const string SectionName = "AuthenticationRateLimiting";

    public int RegistrationPerMinuteLimit { get; init; } = 5;
    public int SignInPerMinuteLimit { get; init; } = 10;
    public int RefreshPerMinuteLimit { get; init; } = 30;
}
