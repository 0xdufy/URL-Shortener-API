using UrlShortener.Application.Interfaces;
using UrlShortener.Application.Dtos;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Infrastructure.Persistence.Repositories;

public class InMemoryShortUrlRepository : IShortUrlRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, ShortUrl> _shortUrlsById = new();
    private readonly Dictionary<string, Guid> _shortUrlIdsByCode = new(StringComparer.Ordinal);
    private readonly Dictionary<(Guid OwnerId, string KeyHash), ShortUrlCreationIdempotencyRecord>
        _idempotencyRecords = new();

    public Task<ShortUrlListResult> ListOwnedAsync(ShortUrlListCriteria criteria, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        lock (_sync)
        {
            var query = _shortUrlsById.Values
                .Where(x => x.OwnerId == criteria.OwnerId);

            if (!criteria.IncludeDeleted)
            {
                query = query.Where(x => !x.IsDeleted);
            }

            if (!string.IsNullOrEmpty(criteria.Search))
            {
                query = query.Where(x =>
                    x.ShortCode.Contains(criteria.Search, StringComparison.Ordinal) ||
                    x.OriginalUrl.Contains(criteria.Search, StringComparison.OrdinalIgnoreCase));
            }

            if (criteria.IsActive.HasValue)
            {
                query = query.Where(x => x.IsActive == criteria.IsActive.Value);
            }

            query = criteria.Expiration switch
            {
                ShortUrlExpirationFilter.Expired => query.Where(x =>
                    x.ExpiresAtUtc.HasValue && x.ExpiresAtUtc.Value <= criteria.NowUtc),
                ShortUrlExpirationFilter.NotExpired => query.Where(x =>
                    !x.ExpiresAtUtc.HasValue || x.ExpiresAtUtc.Value > criteria.NowUtc),
                _ => query
            };

            if (criteria.CreatedFromUtc.HasValue)
            {
                query = query.Where(x => x.CreatedAtUtc >= criteria.CreatedFromUtc.Value);
            }

            if (criteria.CreatedToUtc.HasValue)
            {
                query = query.Where(x => x.CreatedAtUtc <= criteria.CreatedToUtc.Value);
            }

            var filtered = query.ToList();
            var ordered = ApplyOrdering(filtered, criteria.SortBy, criteria.SortDirection);
            var offset = checked((criteria.Page - 1) * criteria.PageSize);
            var items = ordered
                .Skip(offset)
                .Take(criteria.PageSize)
                .Select(x => new ShortUrlListItemResponse
                {
                    Id = x.Id,
                    OriginalUrl = x.OriginalUrl,
                    ShortCode = x.ShortCode,
                    CustomDomainId = x.CustomDomainId,
                    CustomDomainHost = x.CustomDomain?.NormalizedHost,
                    CreatedAtUtc = x.CreatedAtUtc,
                    ExpiresAtUtc = x.ExpiresAtUtc,
                    IsActive = x.IsActive,
                    IsExpired = x.ExpiresAtUtc.HasValue && x.ExpiresAtUtc.Value <= criteria.NowUtc,
                    IsDeleted = x.IsDeleted,
                    DeletedAtUtc = x.DeletedAtUtc,
                    RestoreUntilUtc = x.DeletedAtUtc?.AddDays(criteria.RestoreRetentionDays),
                    ClickCount = x.ClickCount
                })
                .ToList();

            return Task.FromResult(new ShortUrlListResult(items, filtered.Count));
        }
    }

    public Task<ShortUrlCreationResult> TryCreateAsync(ShortUrl entity, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (!IsCustomDomainAvailable(entity))
            {
                return Task.FromResult(ShortUrlCreationResult.CustomDomainUnavailable);
            }

            if (_shortUrlIdsByCode.ContainsKey(entity.ShortCode))
            {
                return Task.FromResult(ShortUrlCreationResult.ShortCodeConflict);
            }

            _shortUrlsById.Add(entity.Id, entity);
            _shortUrlIdsByCode.Add(entity.ShortCode, entity.Id);
            return Task.FromResult(ShortUrlCreationResult.Created);
        }
    }

    public Task<IdempotentShortUrlCreationResult> TryCreateIdempotentAsync(
        ShortUrl entity,
        ShortUrlIdempotencyContext idempotency,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        lock (_sync)
        {
            var expiredKeys = _idempotencyRecords
                .Where(x => x.Value.ExpiresAtUtc <= idempotency.CreatedAtUtc)
                .Select(x => x.Key)
                .ToList();
            foreach (var expiredKey in expiredKeys)
            {
                _idempotencyRecords.Remove(expiredKey);
            }

            var scopedKey = (idempotency.OwnerId, idempotency.KeyHash);
            if (_idempotencyRecords.TryGetValue(scopedKey, out var existing))
            {
                return Task.FromResult(ResolveIdempotencyRecord(existing, idempotency.RequestHash));
            }

            if (_shortUrlIdsByCode.ContainsKey(entity.ShortCode))
            {
                return Task.FromResult(new IdempotentShortUrlCreationResult(
                    IdempotentShortUrlCreationOutcome.ShortCodeConflict));
            }

            if (!IsCustomDomainAvailable(entity))
            {
                return Task.FromResult(new IdempotentShortUrlCreationResult(
                    IdempotentShortUrlCreationOutcome.CustomDomainUnavailable));
            }

            var record = new ShortUrlCreationIdempotencyRecord(
                idempotency.OwnerId,
                idempotency.KeyHash,
                idempotency.RequestHash,
                entity.Id,
                idempotency.CreatedAtUtc,
                idempotency.ExpiresAtUtc)
            {
                Id = Guid.NewGuid()
            };

            _shortUrlsById.Add(entity.Id, entity);
            _shortUrlIdsByCode.Add(entity.ShortCode, entity.Id);
            _idempotencyRecords.Add(scopedKey, record);

            return Task.FromResult(new IdempotentShortUrlCreationResult(
                IdempotentShortUrlCreationOutcome.Created,
                entity));
        }
    }

    public Task<ShortUrl?> GetOwnedByShortCodeNotDeletedAsync(string shortCode, Guid ownerId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (_shortUrlIdsByCode.TryGetValue(shortCode, out var id) &&
                _shortUrlsById.TryGetValue(id, out var entity) &&
                entity.OwnerId == ownerId &&
                !entity.IsDeleted)
            {
                return Task.FromResult<ShortUrl?>(entity);
            }

            return Task.FromResult<ShortUrl?>(null);
        }
    }

    public Task<ShortUrl?> GetOwnedByShortCodeAsync(string shortCode, Guid ownerId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (_shortUrlIdsByCode.TryGetValue(shortCode, out var id) &&
                _shortUrlsById.TryGetValue(id, out var entity) &&
                entity.OwnerId == ownerId)
            {
                return Task.FromResult<ShortUrl?>(entity);
            }

            return Task.FromResult<ShortUrl?>(null);
        }
    }

    public Task<RedirectLookupModel?> GetRedirectAsync(
        RedirectRouteIdentity route,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (_shortUrlIdsByCode.TryGetValue(route.ShortCode, out var id) &&
                _shortUrlsById.TryGetValue(id, out var entity) &&
                MatchesRoute(entity, route))
            {
                return Task.FromResult<RedirectLookupModel?>(new RedirectLookupModel(
                    entity.Id,
                    route.Host,
                    entity.ShortCode,
                    entity.OriginalUrl,
                    entity.ExpiresAtUtc,
                    entity.IsActive,
                    entity.IsDeleted));
            }

            return Task.FromResult<RedirectLookupModel?>(null);
        }
    }

    public Task<bool> IsRedirectCurrentAsync(
        Guid shortUrlId,
        RedirectRouteIdentity route,
        string expectedOriginalUrl,
        DateTime? expectedExpiresAtUtc,
        DateTime accessedAtUtc,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (!_shortUrlsById.TryGetValue(shortUrlId, out var entity))
            {
                return Task.FromResult(false);
            }

            var isCurrent =
                MatchesRoute(entity, route) &&
                string.Equals(entity.OriginalUrl, expectedOriginalUrl, StringComparison.Ordinal) &&
                entity.ExpiresAtUtc == expectedExpiresAtUtc &&
                !entity.IsDeleted &&
                entity.IsActive &&
                (!entity.ExpiresAtUtc.HasValue || entity.ExpiresAtUtc.Value > accessedAtUtc);

            return Task.FromResult(isCurrent);
        }
    }

    public Task<IReadOnlyList<string>> ListShortCodesForCustomDomainAsync(
        Guid customDomainId,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        lock (_sync)
        {
            IReadOnlyList<string> codes = _shortUrlsById.Values
                .Where(x => x.CustomDomainId == customDomainId)
                .Select(x => x.ShortCode)
                .ToList();
            return Task.FromResult(codes);
        }
    }

    public Task<List<(DateTime DateUtc, int Clicks)>> GetDailyClicksAsync(Guid shortUrlId, DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(new List<(DateTime DateUtc, int Clicks)>());
    }

    public Task<AnalyticsSummaryReadModel?> GetAnalyticsSummaryAsync(
        AnalyticsSummaryCriteria criteria,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        lock (_sync)
        {
            var entity = FindOwnedActiveAnalyticsLink(criteria.ShortCode, criteria.OwnerId);
            return Task.FromResult(entity is null
                ? null
                : new AnalyticsSummaryReadModel(
                    entity.ShortCode,
                    0,
                    0,
                    null,
                    [],
                    [],
                    [],
                    []));
        }
    }

    public Task<AnalyticsTimeSeriesReadModel?> GetAnalyticsTimeSeriesAsync(
        AnalyticsTimeSeriesCriteria criteria,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        lock (_sync)
        {
            var entity = FindOwnedActiveAnalyticsLink(criteria.ShortCode, criteria.OwnerId);
            return Task.FromResult(entity is null
                ? null
                : new AnalyticsTimeSeriesReadModel(entity.ShortCode, []));
        }
    }

    public Task SaveChangesAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    private static bool IsCustomDomainAvailable(ShortUrl entity) =>
        !entity.CustomDomainId.HasValue ||
        entity.CustomDomain is { CanServeBrandedLinks: true } customDomain &&
        customDomain.Id == entity.CustomDomainId.Value &&
        customDomain.OwnerId == entity.OwnerId;

    private static bool MatchesRoute(ShortUrl entity, RedirectRouteIdentity route) =>
        route.IsDefaultHost
            ? entity.CustomDomainId == null
            : entity.CustomDomain is { CanServeBrandedLinks: true } customDomain &&
                customDomain.NormalizedHost.Equals(route.Host, StringComparison.Ordinal);

    private ShortUrl? FindOwnedActiveAnalyticsLink(string shortCode, Guid ownerId)
    {
        return _shortUrlIdsByCode.TryGetValue(shortCode, out var id) &&
            _shortUrlsById.TryGetValue(id, out var entity) &&
            entity.OwnerId == ownerId &&
            !entity.IsDeleted
                ? entity
                : null;
    }

    private IdempotentShortUrlCreationResult ResolveIdempotencyRecord(
        ShortUrlCreationIdempotencyRecord record,
        string requestHash)
    {
        if (!string.Equals(record.RequestHash, requestHash, StringComparison.Ordinal))
        {
            return new IdempotentShortUrlCreationResult(
                IdempotentShortUrlCreationOutcome.RequestConflict);
        }

        return _shortUrlsById.TryGetValue(record.ShortUrlId, out var shortUrl)
            ? new IdempotentShortUrlCreationResult(
                IdempotentShortUrlCreationOutcome.Existing,
                shortUrl)
            : throw new InvalidOperationException("An idempotency record references a missing short URL.");
    }

    private static IOrderedEnumerable<ShortUrl> ApplyOrdering(
        IEnumerable<ShortUrl> query,
        ShortUrlSortField sortBy,
        SortDirection direction)
    {
        if (direction == SortDirection.Ascending)
        {
            return sortBy switch
            {
                ShortUrlSortField.ShortCode => query.OrderBy(x => x.ShortCode, StringComparer.Ordinal).ThenBy(x => x.Id),
                ShortUrlSortField.ClickCount => query.OrderBy(x => x.ClickCount).ThenBy(x => x.Id),
                ShortUrlSortField.ExpiresAt => query.OrderBy(x => x.ExpiresAtUtc).ThenBy(x => x.Id),
                _ => query.OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id)
            };
        }

        return sortBy switch
        {
            ShortUrlSortField.ShortCode => query.OrderByDescending(x => x.ShortCode, StringComparer.Ordinal).ThenByDescending(x => x.Id),
            ShortUrlSortField.ClickCount => query.OrderByDescending(x => x.ClickCount).ThenByDescending(x => x.Id),
            ShortUrlSortField.ExpiresAt => query.OrderByDescending(x => x.ExpiresAtUtc).ThenByDescending(x => x.Id),
            _ => query.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id)
        };
    }
}
