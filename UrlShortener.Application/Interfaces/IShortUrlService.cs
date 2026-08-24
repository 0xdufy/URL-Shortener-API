using UrlShortener.Application.Dtos;

namespace UrlShortener.Application.Interfaces;

public interface IShortUrlService
{
    Task<ShortUrlListResponse> ListAsync(ShortUrlListQuery query, CancellationToken ct);
    Task<ShortUrlResponse> CreateAsync(
        CreateShortUrlRequest req,
        string? idempotencyKey,
        CancellationToken ct);
    Task<ShortUrlResponse?> GetAsync(string shortCode, CancellationToken ct);
    Task<ShortUrlResponse?> UpdateAsync(string shortCode, UpdateShortUrlRequest request, CancellationToken ct);
    Task<ShortUrlResponse?> SetStatusAsync(string shortCode, bool isActive, CancellationToken ct);
    Task<bool> DeleteAsync(string shortCode, CancellationToken ct);
    Task<ShortUrlResponse> RestoreAsync(string shortCode, CancellationToken ct);
    Task<StatsResponse?> GetStatsAsync(string shortCode, DateTime? fromUtc, DateTime? toUtc, CancellationToken ct);
}
