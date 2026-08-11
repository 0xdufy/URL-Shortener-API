using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Application.Interfaces;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Infrastructure.Persistence.Repositories;

public class ShortUrlRepository : IShortUrlRepository
{
    private const string CaseSensitiveCollation = "Latin1_General_CS_AS";
    private const string ShortCodeUniqueIndexName = "IX_ShortUrls_ShortCode";
    private readonly AppDbContext _dbContext;

    public ShortUrlRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ShortUrlCreationResult> TryCreateAsync(ShortUrl entity, CancellationToken ct)
    {
        await _dbContext.ShortUrls.AddAsync(entity, ct);

        try
        {
            await _dbContext.SaveChangesAsync(ct);
            return ShortUrlCreationResult.Created;
        }
        catch (DbUpdateException exception) when (IsShortCodeConflict(exception))
        {
            _dbContext.Entry(entity).State = EntityState.Detached;
            return ShortUrlCreationResult.ShortCodeConflict;
        }
    }

    public Task<ShortUrl?> GetOwnedByShortCodeNotDeletedAsync(string shortCode, Guid ownerId, CancellationToken ct)
    {
        return _dbContext.ShortUrls
            .FirstOrDefaultAsync(
                x => EF.Functions.Collate(x.ShortCode, CaseSensitiveCollation) == shortCode &&
                    x.OwnerId == ownerId &&
                    !x.IsDeleted,
                ct);
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

    private static bool IsShortCodeConflict(DbUpdateException exception)
    {
        if (exception.InnerException is not SqlException sqlException)
        {
            return false;
        }

        return sqlException.Errors
            .Cast<SqlError>()
            .Any(error =>
                error.Number is 2601 or 2627 &&
                error.Message.Contains(ShortCodeUniqueIndexName, StringComparison.OrdinalIgnoreCase));
    }
}
