namespace UrlShortener.Infrastructure.Configuration;

public enum RateLimitAlgorithm
{
    FixedWindow,
    SlidingWindow,
    TokenBucket
}
