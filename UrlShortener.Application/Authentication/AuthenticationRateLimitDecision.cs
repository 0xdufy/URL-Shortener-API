namespace UrlShortener.Application.Authentication;

public sealed record AuthenticationRateLimitDecision(bool IsAllowed, int RetryAfterSeconds);
