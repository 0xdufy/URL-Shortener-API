using UrlShortener.Application.RateLimiting;

namespace UrlShortener.Application.Interfaces;

public interface IDistributedRateLimiter
{
    Task<RateLimitDecision> CheckAsync(
        RateLimitPolicy policy,
        string partitionKey,
        CancellationToken cancellationToken);
}
