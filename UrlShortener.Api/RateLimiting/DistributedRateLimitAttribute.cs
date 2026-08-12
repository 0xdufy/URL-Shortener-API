using UrlShortener.Application.RateLimiting;

namespace UrlShortener.Api.RateLimiting;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class DistributedRateLimitAttribute : Attribute
{
    public DistributedRateLimitAttribute(RateLimitPolicy policy)
    {
        Policy = policy;
    }

    public RateLimitPolicy Policy { get; }
}
