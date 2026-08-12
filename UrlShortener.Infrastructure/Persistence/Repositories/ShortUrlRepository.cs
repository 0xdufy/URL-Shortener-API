using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Application.Dtos;
using UrlShortener.Application.Interfaces;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Infrastructure.Persistence.Repositories;

public class ShortUrlRepository : IShortUrlRepository
{
    private const string CaseSensitiveCollation = "Latin1_General_CS_AS";
    private const string BinaryCollation = "Latin1_General_100_BIN2";
    private const string ShortCodeUniqueIndexName = "IX_ShortUrls_ShortCode";
    private readonly AppDbContext _dbContext;

    public ShortUrlRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ShortUrlListResult> ListOwnedAsync(ShortUrlListCriteria criteria, CancellationToken ct)
    {
        var query = _dbContext.ShortUrls
            .AsNoTracking()
            .Where(x => x.OwnerId == criteria.OwnerId);

        if (!criteria.IncludeDeleted)
        {
            query = query.Where(x => !x.IsDeleted);
        }

        if (!string.IsNullOrEmpty(criteria.Search))
        {
            query = query.Where(x =>
                x.ShortCode.Contains(criteria.Search) ||
                x.OriginalUrl.Contains(criteria.Search));
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

        var totalItems = await query.CountAsync(ct);
        var orderedQuery = ApplyOrdering(query, criteria.SortBy, criteria.SortDirection);
        var offset = checked((criteria.Page - 1) * criteria.PageSize);

        var items = await orderedQuery
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
                DeletedAtUtc = x.DeletedAtUtc,
                RestoreUntilUtc = x.DeletedAtUtc.HasValue
                    ? x.DeletedAtUtc.Value.AddDays(criteria.RestoreRetentionDays)
                    : null,
                ClickCount = x.ClickCount
            })
            .ToListAsync(ct);

        return new ShortUrlListResult(items, totalItems);
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

    public Task<ShortUrl?> GetOwnedByShortCodeAsync(string shortCode, Guid ownerId, CancellationToken ct)
    {
        return _dbContext.ShortUrls
            .FirstOrDefaultAsync(
                x => EF.Functions.Collate(x.ShortCode, CaseSensitiveCollation) == shortCode &&
                    x.OwnerId == ownerId,
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

    public async Task<bool> IncrementClickCountAsync(
        Guid shortUrlId,
        string expectedOriginalUrl,
        DateTime? expectedExpiresAtUtc,
        DateTime accessedAtUtc,
        CancellationToken ct)
    {
        var affectedRows = await _dbContext.ShortUrls
            .Where(x =>
                x.Id == shortUrlId &&
                EF.Functions.Collate(x.OriginalUrl, BinaryCollation) == expectedOriginalUrl &&
                x.ExpiresAtUtc == expectedExpiresAtUtc &&
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

    private static IOrderedQueryable<ShortUrl> ApplyOrdering(
        IQueryable<ShortUrl> query,
        ShortUrlSortField sortBy,
        SortDirection direction)
    {
        if (direction == SortDirection.Ascending)
        {
            return sortBy switch
            {
                ShortUrlSortField.ShortCode => query.OrderBy(x => x.ShortCode).ThenBy(x => x.Id),
                ShortUrlSortField.ClickCount => query.OrderBy(x => x.ClickCount).ThenBy(x => x.Id),
                ShortUrlSortField.ExpiresAt => query.OrderBy(x => x.ExpiresAtUtc).ThenBy(x => x.Id),
                _ => query.OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id)
            };
        }

        return sortBy switch
        {
            ShortUrlSortField.ShortCode => query.OrderByDescending(x => x.ShortCode).ThenByDescending(x => x.Id),
            ShortUrlSortField.ClickCount => query.OrderByDescending(x => x.ClickCount).ThenByDescending(x => x.Id),
            ShortUrlSortField.ExpiresAt => query.OrderByDescending(x => x.ExpiresAtUtc).ThenByDescending(x => x.Id),
            _ => query.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id)
        };
    }
}
