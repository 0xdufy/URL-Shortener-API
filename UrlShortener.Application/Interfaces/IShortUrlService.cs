using UrlShortener.Application.Dtos;

namespace UrlShortener.Application.Interfaces;

public interface IShortUrlService
{
    Task<ShortUrlListResponse> ListAsync(ShortUrlListQuery query, CancellationToken ct);
    Task<ShortUrlResponse> CreateAsync(CreateShortUrlRequest req, string baseHost, string clientIp, CancellationToken ct);
    Task<ShortUrlDetailsResponse?> GetAsync(string shortCode, CancellationToken ct);
    Task<ShortUrlDetailsResponse?> UpdateAsync(string shortCode, UpdateShortUrlRequest request, CancellationToken ct);
    Task<ShortUrlDetailsResponse?> SetStatusAsync(string shortCode, bool isActive, CancellationToken ct);
    Task<bool> DeleteAsync(string shortCode, CancellationToken ct);
    Task<ShortUrlDetailsResponse> RestoreAsync(string shortCode, CancellationToken ct);
    Task<StatsResponse?> GetStatsAsync(string shortCode, DateTime? fromUtc, DateTime? toUtc, CancellationToken ct);
    Task<(int statusCode, string? originalUrl)> ResolveForRedirectAsync(string shortCode, string ip, string? userAgent, string? referer, CancellationToken ct);
}
