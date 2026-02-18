namespace UrlShortener.Application.Interfaces;

public interface IRateLimiter
{
    bool IsAllowed(string ip, DateTime utcNow, out int remaining, out int retryAfterSeconds);
}
