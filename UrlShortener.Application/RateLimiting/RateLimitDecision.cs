namespace UrlShortener.Application.RateLimiting;

public sealed record RateLimitDecision(
    bool IsAllowed,
    int Remaining,
    int RetryAfterSeconds);
