using UrlShortener.Application.Dtos;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Application.Interfaces;

public interface IShortUrlRepository
{
    Task<ShortUrlListResult> ListOwnedAsync(ShortUrlListCriteria criteria, CancellationToken ct);
    Task<ShortUrlCreationResult> TryCreateAsync(ShortUrl entity, CancellationToken ct);
    Task<IdempotentShortUrlCreationResult> TryCreateIdempotentAsync(
        ShortUrl entity,
        ShortUrlIdempotencyContext idempotency,
        CancellationToken ct);
    Task<ShortUrl?> GetOwnedByShortCodeNotDeletedAsync(string shortCode, Guid ownerId, CancellationToken ct);
    Task<ShortUrl?> GetOwnedByShortCodeAsync(string shortCode, Guid ownerId, CancellationToken ct);
    Task<ShortUrl?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddModerationActionAsync(ShortUrlModerationAction action, CancellationToken ct);
    Task<RedirectLookupModel?> GetRedirectAsync(RedirectRouteIdentity route, CancellationToken ct);
    Task<bool> IsRedirectCurrentAsync(
        Guid shortUrlId,
        RedirectRouteIdentity route,
        string expectedOriginalUrl,
        DateTime? expectedExpiresAtUtc,
        DateTime accessedAtUtc,
        CancellationToken ct);
    Task<IReadOnlyList<string>> ListShortCodesForCustomDomainAsync(Guid customDomainId, CancellationToken ct);
    Task<List<(DateTime DateUtc, int Clicks)>> GetDailyClicksAsync(Guid shortUrlId, DateTime fromUtc, DateTime toUtc, CancellationToken ct);
    Task<AnalyticsSummaryReadModel?> GetAnalyticsSummaryAsync(AnalyticsSummaryCriteria criteria, CancellationToken ct);
    Task<AnalyticsTimeSeriesReadModel?> GetAnalyticsTimeSeriesAsync(AnalyticsTimeSeriesCriteria criteria, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
