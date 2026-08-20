using UrlShortener.Application.Dtos;

namespace UrlShortener.Application.Interfaces;

public interface IShortUrlCache
{
    Task<ShortUrlCacheModel?> GetAsync(string routingHost, string shortCode, CancellationToken ct);
    Task SetAsync(
        string routingHost,
        string shortCode,
        ShortUrlCacheModel model,
        DateTime absoluteExpirationUtc,
        CancellationToken ct);
    Task RemoveAsync(string routingHost, string shortCode, CancellationToken ct);
}
