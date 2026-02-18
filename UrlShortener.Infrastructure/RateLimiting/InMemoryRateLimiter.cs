using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using UrlShortener.Application.Interfaces;

namespace UrlShortener.Infrastructure.RateLimiting;

public class InMemoryRateLimiter : IRateLimiter
{
    private readonly IMemoryCache _memoryCache;
    private readonly int _createPerMinuteLimit;
    private readonly object _lock = new();

    public InMemoryRateLimiter(IMemoryCache memoryCache, IConfiguration configuration)
    {
        _memoryCache = memoryCache;
        var configuredValue = configuration["RateLimiting:CreatePerMinuteLimit"];
        _createPerMinuteLimit = int.TryParse(configuredValue, out var limit) ? limit : 20;
    }

    public bool IsAllowed(string ip, DateTime utcNow, out int remaining, out int retryAfterSeconds)
    {
        var currentMinuteKey = utcNow.ToString("yyyyMMddHHmm");
        var key = $"rl:create:{ip}:{currentMinuteKey}";
        var nextMinute = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, utcNow.Hour, utcNow.Minute, 0, DateTimeKind.Utc).AddMinutes(1);
        retryAfterSeconds = (int)Math.Ceiling((nextMinute - utcNow).TotalSeconds);
        if (retryAfterSeconds < 1)
        {
            retryAfterSeconds = 1;
        }

        lock (_lock)
        {
            var count = _memoryCache.Get<int?>(key) ?? 0;

            if (count >= _createPerMinuteLimit)
            {
                remaining = 0;
                return false;
            }

            count += 1;
            remaining = _createPerMinuteLimit - count;
            _memoryCache.Set(key, count, new DateTimeOffset(nextMinute));
            return true;
        }
    }
}
