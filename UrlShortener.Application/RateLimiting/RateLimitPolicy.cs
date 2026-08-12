namespace UrlShortener.Application.RateLimiting;

public enum RateLimitPolicy
{
    Anonymous,
    AuthenticationRegistration,
    AuthenticationSignIn,
    AuthenticationSession,
    Authenticated,
    UrlCreation,
    ApiKey
}
