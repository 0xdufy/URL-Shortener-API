namespace UrlShortener.Infrastructure.Configuration;

public sealed class RateLimitPolicyOptions
{
    public RateLimitAlgorithm Algorithm { get; init; }
    public int PermitLimit { get; init; }
    public int WindowSeconds { get; init; }
    public int TokensPerPeriod { get; init; }
    public int ReplenishmentPeriodSeconds { get; init; }
}
