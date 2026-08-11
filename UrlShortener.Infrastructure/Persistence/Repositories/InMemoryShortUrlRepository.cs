using UrlShortener.Application.Interfaces;
using UrlShortener.Application.Dtos;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Infrastructure.Persistence.Repositories;

public class InMemoryShortUrlRepository : IShortUrlRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, ShortUrl> _shortUrlsById = new();
    private readonly Dictionary<string, Guid> _shortUrlIdsByCode = new(StringComparer.Ordinal);
    private readonly List<ShortUrlAccessLog> _accessLogs = new();

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
                    CreatedAtUtc = x.CreatedAtUtc,
                    ExpiresAtUtc = x.ExpiresAtUtc,
                    IsActive = x.IsActive,
                    IsExpired = x.ExpiresAtUtc.HasValue && x.ExpiresAtUtc.Value <= criteria.NowUtc,
                    IsDeleted = x.IsDeleted,
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
            if (_shortUrlIdsByCode.ContainsKey(entity.ShortCode))
            {
                return Task.FromResult(ShortUrlCreationResult.ShortCodeConflict);
            }

            _shortUrlsById.Add(entity.Id, entity);
            _shortUrlIdsByCode.Add(entity.ShortCode, entity.Id);
            return Task.FromResult(ShortUrlCreationResult.Created);
        }
    }

    public Task<ShortUrl?> GetOwnedByShortCodeNotDeletedAsync(string shortCode, Guid ownerId, CancellationToken ct)
    {
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

    public Task<ShortUrl?> GetByShortCodeAnyAsync(string shortCode, CancellationToken ct)
    {
        lock (_sync)
        {
            if (_shortUrlIdsByCode.TryGetValue(shortCode, out var id) &&
                _shortUrlsById.TryGetValue(id, out var entity))
            {
                return Task.FromResult<ShortUrl?>(entity);
            }

            return Task.FromResult<ShortUrl?>(null);
        }
    }

    public Task<List<(DateTime DateUtc, int Clicks)>> GetDailyClicksAsync(Guid shortUrlId, DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        lock (_sync)
        {
            var result = _accessLogs
                .Where(x => x.ShortUrlId == shortUrlId && x.AccessedAtUtc >= fromUtc && x.AccessedAtUtc <= toUtc)
                .GroupBy(x => x.AccessedAtUtc.Date)
                .OrderBy(x => x.Key)
                .Select(x => (x.Key, x.Count()))
                .ToList();

            return Task.FromResult(result);
        }
    }

    public Task<bool> IncrementClickCountAsync(Guid shortUrlId, DateTime accessedAtUtc, CancellationToken ct)
    {
        lock (_sync)
        {
            if (!_shortUrlsById.TryGetValue(shortUrlId, out var entity))
            {
                return Task.FromResult(false);
            }

            if (entity.IsDeleted || !entity.IsActive)
            {
                return Task.FromResult(false);
            }

            if (entity.ExpiresAtUtc.HasValue && entity.ExpiresAtUtc.Value <= accessedAtUtc)
            {
                return Task.FromResult(false);
            }

            entity.ClickCount += 1;
            entity.LastAccessedAtUtc = accessedAtUtc;
            return Task.FromResult(true);
        }
    }

    public Task AddAccessLogAsync(ShortUrlAccessLog log, CancellationToken ct)
    {
        lock (_sync)
        {
            _accessLogs.Add(log);
        }

        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct)
    {
        return Task.CompletedTask;
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
