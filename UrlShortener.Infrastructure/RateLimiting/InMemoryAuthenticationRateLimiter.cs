using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using UrlShortener.Application.Authentication;
using UrlShortener.Application.Interfaces;
using UrlShortener.Infrastructure.Configuration;

namespace UrlShortener.Infrastructure.RateLimiting;

public sealed class InMemoryAuthenticationRateLimiter : IAuthenticationRateLimiter
{
    private readonly IMemoryCache _memoryCache;
    private readonly AuthenticationRateLimitingOptions _options;
    private readonly object _lock = new();

    public InMemoryAuthenticationRateLimiter(
        IMemoryCache memoryCache,
        IOptions<AuthenticationRateLimitingOptions> options)
    {
        _memoryCache = memoryCache;
        _options = options.Value;
    }

    public AuthenticationRateLimitDecision Check(
        AuthenticationOperation operation,
        string partitionKey,
        DateTime nowUtc)
    {
        var limit = operation switch
        {
            AuthenticationOperation.Register => _options.RegistrationPerMinuteLimit,
            AuthenticationOperation.SignIn => _options.SignInPerMinuteLimit,
            AuthenticationOperation.Refresh => _options.RefreshPerMinuteLimit,
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
        };

        var windowStart = new DateTime(
            nowUtc.Year,
            nowUtc.Month,
            nowUtc.Day,
            nowUtc.Hour,
            nowUtc.Minute,
            0,
            DateTimeKind.Utc);
        var windowEnd = windowStart.AddMinutes(1);
        var retryAfterSeconds = Math.Max(1, (int)Math.Ceiling((windowEnd - nowUtc).TotalSeconds));
        var key = $"auth:{operation}:{partitionKey}:{windowStart:yyyyMMddHHmm}";

        lock (_lock)
        {
            var count = _memoryCache.Get<int?>(key) ?? 0;
            if (count >= limit)
            {
                return new AuthenticationRateLimitDecision(false, retryAfterSeconds);
            }

            _memoryCache.Set(key, count + 1, new DateTimeOffset(windowEnd));
            return new AuthenticationRateLimitDecision(true, retryAfterSeconds);
        }
    }
}
