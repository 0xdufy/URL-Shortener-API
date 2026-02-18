using UrlShortener.Domain.Entities;

namespace UrlShortener.Application.Interfaces;

public interface IShortUrlRepository
{
    Task<bool> ShortCodeExistsAsync(string shortCode, CancellationToken ct);
    Task AddShortUrlAsync(ShortUrl entity, CancellationToken ct);
    Task<ShortUrl?> GetByShortCodeNotDeletedAsync(string shortCode, CancellationToken ct);
    Task<ShortUrl?> GetByShortCodeAnyAsync(string shortCode, CancellationToken ct);
    Task<List<ShortUrlAccessLog>> GetAccessLogsAsync(Guid shortUrlId, DateTime fromUtc, DateTime toUtc, CancellationToken ct);
    Task AddAccessLogAsync(ShortUrlAccessLog log, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
