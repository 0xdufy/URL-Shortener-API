using Microsoft.EntityFrameworkCore;
using UrlShortener.Application.Interfaces;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Infrastructure.Persistence.Repositories;

public class ShortUrlRepository : IShortUrlRepository
{
    private const string CaseSensitiveCollation = "Latin1_General_CS_AS";
    private readonly AppDbContext _dbContext;

    public ShortUrlRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ShortCodeExistsAsync(string shortCode, CancellationToken ct)
    {
        return _dbContext.ShortUrls
            .AnyAsync(x => EF.Functions.Collate(x.ShortCode, CaseSensitiveCollation) == shortCode, ct);
    }

    public Task AddShortUrlAsync(ShortUrl entity, CancellationToken ct)
    {
        return _dbContext.ShortUrls.AddAsync(entity, ct).AsTask();
    }

    public Task<ShortUrl?> GetByShortCodeNotDeletedAsync(string shortCode, CancellationToken ct)
    {
        return _dbContext.ShortUrls
            .FirstOrDefaultAsync(x => EF.Functions.Collate(x.ShortCode, CaseSensitiveCollation) == shortCode && !x.IsDeleted, ct);
    }

    public Task<ShortUrl?> GetByShortCodeAnyAsync(string shortCode, CancellationToken ct)
    {
        return _dbContext.ShortUrls
            .FirstOrDefaultAsync(x => EF.Functions.Collate(x.ShortCode, CaseSensitiveCollation) == shortCode, ct);
    }

    public async Task<List<(DateTime DateUtc, int Clicks)>> GetDailyClicksAsync(Guid shortUrlId, DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        var grouped = await _dbContext.ShortUrlAccessLogs
            .AsNoTracking()
            .Where(x => x.ShortUrlId == shortUrlId && x.AccessedAtUtc >= fromUtc && x.AccessedAtUtc <= toUtc)
            .GroupBy(x => x.AccessedAtUtc.Date)
            .Select(x => new
            {
                DateUtc = x.Key,
                Clicks = x.Count()
            })
            .OrderBy(x => x.DateUtc)
            .ToListAsync(ct);

        return grouped.Select(x => (x.DateUtc, x.Clicks)).ToList();
    }

    public async Task<bool> IncrementClickCountAsync(Guid shortUrlId, DateTime accessedAtUtc, CancellationToken ct)
    {
        var affectedRows = await _dbContext.ShortUrls
            .Where(x =>
                x.Id == shortUrlId &&
                !x.IsDeleted &&
                x.IsActive &&
                (!x.ExpiresAtUtc.HasValue || x.ExpiresAtUtc.Value > accessedAtUtc))
            .ExecuteUpdateAsync(update => update
                .SetProperty(x => x.ClickCount, x => x.ClickCount + 1)
                .SetProperty(x => x.LastAccessedAtUtc, accessedAtUtc), ct);

        return affectedRows > 0;
    }

    public Task AddAccessLogAsync(ShortUrlAccessLog log, CancellationToken ct)
    {
        return _dbContext.ShortUrlAccessLogs.AddAsync(log, ct).AsTask();
    }

    public Task SaveChangesAsync(CancellationToken ct)
    {
        return _dbContext.SaveChangesAsync(ct);
    }
}
