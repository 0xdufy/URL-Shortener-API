using UrlShortener.Application.Dtos;

namespace UrlShortener.Application.Interfaces;

public interface IShortUrlModerationService
{
    Task<ShortUrlModerationResponse?> ModerateAsync(
        Guid shortUrlId,
        ModerateShortUrlRequest request,
        CancellationToken ct);
}
