using UrlShortener.Application.Authentication;

namespace UrlShortener.Application.Interfaces;

public interface IAuthenticationRateLimiter
{
    AuthenticationRateLimitDecision Check(
        AuthenticationOperation operation,
        string partitionKey,
        DateTime nowUtc);
}
