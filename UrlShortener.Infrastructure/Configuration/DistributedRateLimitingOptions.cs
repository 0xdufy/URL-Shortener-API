namespace UrlShortener.Infrastructure.Configuration;

public sealed class DistributedRateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public RateLimitPolicyOptions Anonymous { get; init; } = new()
    {
        Algorithm = RateLimitAlgorithm.FixedWindow,
        PermitLimit = 120,
        WindowSeconds = 60
    };

    public RateLimitPolicyOptions AuthenticationRegistration { get; init; } = new()
    {
        Algorithm = RateLimitAlgorithm.SlidingWindow,
        PermitLimit = 5,
        WindowSeconds = 60
    };

    public RateLimitPolicyOptions AuthenticationSignIn { get; init; } = new()
    {
        Algorithm = RateLimitAlgorithm.SlidingWindow,
        PermitLimit = 10,
        WindowSeconds = 60
    };

    public RateLimitPolicyOptions AuthenticationSession { get; init; } = new()
    {
        Algorithm = RateLimitAlgorithm.SlidingWindow,
        PermitLimit = 30,
        WindowSeconds = 60
    };

    public RateLimitPolicyOptions Authenticated { get; init; } = new()
    {
        Algorithm = RateLimitAlgorithm.FixedWindow,
        PermitLimit = 300,
        WindowSeconds = 60
    };

    public RateLimitPolicyOptions UrlCreation { get; init; } = new()
    {
        Algorithm = RateLimitAlgorithm.TokenBucket,
        PermitLimit = 20,
        TokensPerPeriod = 20,
        ReplenishmentPeriodSeconds = 60
    };

    public RateLimitPolicyOptions ApiKey { get; init; } = new()
    {
        Algorithm = RateLimitAlgorithm.TokenBucket,
        PermitLimit = 600,
        TokensPerPeriod = 600,
        ReplenishmentPeriodSeconds = 60
    };

    public IEnumerable<(string Name, RateLimitPolicyOptions Options)> GetPolicies()
    {
        yield return (nameof(Anonymous), Anonymous);
        yield return (nameof(AuthenticationRegistration), AuthenticationRegistration);
        yield return (nameof(AuthenticationSignIn), AuthenticationSignIn);
        yield return (nameof(AuthenticationSession), AuthenticationSession);
        yield return (nameof(Authenticated), Authenticated);
        yield return (nameof(UrlCreation), UrlCreation);
        yield return (nameof(ApiKey), ApiKey);
    }
}
