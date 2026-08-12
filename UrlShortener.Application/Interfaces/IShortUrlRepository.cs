using UrlShortener.Domain.Entities;

namespace UrlShortener.Application.Interfaces;

public interface IShortUrlRepository
{
    Task<ShortUrlListResult> ListOwnedAsync(ShortUrlListCriteria criteria, CancellationToken ct);
    Task<ShortUrlCreationResult> TryCreateAsync(ShortUrl entity, CancellationToken ct);
    Task<ShortUrl?> GetOwnedByShortCodeNotDeletedAsync(string shortCode, Guid ownerId, CancellationToken ct);
    Task<ShortUrl?> GetOwnedByShortCodeAsync(string shortCode, Guid ownerId, CancellationToken ct);
    Task<ShortUrl?> GetByShortCodeAnyAsync(string shortCode, CancellationToken ct);
    Task<List<(DateTime DateUtc, int Clicks)>> GetDailyClicksAsync(Guid shortUrlId, DateTime fromUtc, DateTime toUtc, CancellationToken ct);
    Task<bool> IncrementClickCountAsync(
        Guid shortUrlId,
        string expectedOriginalUrl,
        DateTime? expectedExpiresAtUtc,
        DateTime accessedAtUtc,
        CancellationToken ct);
    Task AddAccessLogAsync(ShortUrlAccessLog log, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
