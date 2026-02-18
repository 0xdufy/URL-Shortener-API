using UrlShortener.Application.Interfaces;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Infrastructure.Persistence.Repositories;

public class InMemoryShortUrlRepository : IShortUrlRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, ShortUrl> _shortUrlsById = new();
    private readonly Dictionary<string, Guid> _shortUrlIdsByCode = new(StringComparer.Ordinal);
    private readonly List<ShortUrlAccessLog> _accessLogs = new();

    public Task<bool> ShortCodeExistsAsync(string shortCode, CancellationToken ct)
    {
        lock (_sync)
        {
            return Task.FromResult(_shortUrlIdsByCode.ContainsKey(shortCode));
        }
    }

    public Task AddShortUrlAsync(ShortUrl entity, CancellationToken ct)
    {
        lock (_sync)
        {
            _shortUrlsById[entity.Id] = entity;
            _shortUrlIdsByCode[entity.ShortCode] = entity.Id;
        }

        return Task.CompletedTask;
    }

    public Task<ShortUrl?> GetByShortCodeNotDeletedAsync(string shortCode, CancellationToken ct)
    {
        lock (_sync)
        {
            if (_shortUrlIdsByCode.TryGetValue(shortCode, out var id) &&
                _shortUrlsById.TryGetValue(id, out var entity) &&
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
}
