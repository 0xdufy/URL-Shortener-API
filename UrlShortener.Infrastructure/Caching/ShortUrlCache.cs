using Microsoft.Extensions.Caching.Memory;
using UrlShortener.Application.Dtos;
using UrlShortener.Application.Interfaces;

namespace UrlShortener.Infrastructure.Caching;

public class ShortUrlCache : IShortUrlCache
{
    private readonly IMemoryCache _memoryCache;

    public ShortUrlCache(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public Task<ShortUrlCacheModel?> GetAsync(string shortCode, CancellationToken ct)
    {
        _memoryCache.TryGetValue(GetKey(shortCode), out ShortUrlCacheModel? model);
        return Task.FromResult(model);
    }

    public Task SetAsync(string shortCode, ShortUrlCacheModel model, TimeSpan ttl, CancellationToken ct)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        };

        _memoryCache.Set(GetKey(shortCode), model, options);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string shortCode, CancellationToken ct)
    {
        _memoryCache.Remove(GetKey(shortCode));
        return Task.CompletedTask;
    }

    private static string GetKey(string shortCode)
    {
        return $"su:{shortCode}";
    }
}
