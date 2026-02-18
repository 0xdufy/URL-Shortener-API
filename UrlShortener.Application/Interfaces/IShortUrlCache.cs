using UrlShortener.Application.Dtos;

namespace UrlShortener.Application.Interfaces;

public interface IShortUrlCache
{
    Task<ShortUrlCacheModel?> GetAsync(string shortCode, CancellationToken ct);
    Task SetAsync(string shortCode, ShortUrlCacheModel model, TimeSpan ttl, CancellationToken ct);
    Task RemoveAsync(string shortCode, CancellationToken ct);
}
