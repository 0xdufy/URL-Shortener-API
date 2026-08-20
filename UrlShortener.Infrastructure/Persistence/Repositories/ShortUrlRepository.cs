using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Application.Dtos;
using UrlShortener.Application.Interfaces;
using UrlShortener.Domain.Analytics;
using UrlShortener.Domain.Entities;
using UrlShortener.Domain.CustomDomains;

namespace UrlShortener.Infrastructure.Persistence.Repositories;

public class ShortUrlRepository : IShortUrlRepository
{
    private const string CaseSensitiveCollation = "Latin1_General_CS_AS";
    private const string BinaryCollation = "Latin1_General_100_BIN2";
    private const string ShortCodeUniqueIndexName = "IX_ShortUrls_ShortCode";
    private const string IdempotencyUniqueIndexName =
        "IX_ShortUrlCreationIdempotencyRecords_OwnerId_KeyHash";
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
                CustomDomainId = x.CustomDomainId,
                CustomDomainHost = x.CustomDomain == null ? null : x.CustomDomain.NormalizedHost,
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
        if (!await IsCustomDomainAvailableAsync(entity, ct))
        {
            return ShortUrlCreationResult.CustomDomainUnavailable;
        }

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

    public async Task<IdempotentShortUrlCreationResult> TryCreateIdempotentAsync(
        ShortUrl entity,
        ShortUrlIdempotencyContext idempotency,
        CancellationToken ct)
    {
        await DeleteExpiredIdempotencyRecordsAsync(idempotency.CreatedAtUtc, ct);

        var existing = await FindIdempotencyRecordAsync(idempotency, ct);
        if (existing != null)
        {
            return ResolveIdempotencyRecord(existing, idempotency.RequestHash);
        }

        if (!await IsCustomDomainAvailableAsync(entity, ct))
        {
            return new IdempotentShortUrlCreationResult(
                IdempotentShortUrlCreationOutcome.CustomDomainUnavailable);
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

        await _dbContext.ShortUrls.AddAsync(entity, ct);
        await _dbContext.ShortUrlCreationIdempotencyRecords.AddAsync(record, ct);

        try
        {
            await _dbContext.SaveChangesAsync(ct);
            return new IdempotentShortUrlCreationResult(
                IdempotentShortUrlCreationOutcome.Created,
                entity);
        }
        catch (DbUpdateException exception) when (IsShortCodeConflict(exception))
        {
            DetachFailedIdempotentCreation(entity, record);
            existing = await FindIdempotencyRecordAsync(idempotency, ct);
            if (existing != null)
            {
                return ResolveIdempotencyRecord(existing, idempotency.RequestHash);
            }

            return new IdempotentShortUrlCreationResult(
                IdempotentShortUrlCreationOutcome.ShortCodeConflict);
        }
        catch (DbUpdateException exception) when (IsIdempotencyConflict(exception))
        {
            DetachFailedIdempotentCreation(entity, record);
            existing = await FindIdempotencyRecordAsync(idempotency, ct);
            if (existing == null)
            {
                throw;
            }

            return ResolveIdempotencyRecord(existing, idempotency.RequestHash);
        }
    }

    public Task<ShortUrl?> GetOwnedByShortCodeNotDeletedAsync(string shortCode, Guid ownerId, CancellationToken ct)
    {
        return _dbContext.ShortUrls
            .Include(x => x.CustomDomain)
            .FirstOrDefaultAsync(
                x => EF.Functions.Collate(x.ShortCode, CaseSensitiveCollation) == shortCode &&
                    x.OwnerId == ownerId &&
                    !x.IsDeleted,
                ct);
    }

    public Task<ShortUrl?> GetOwnedByShortCodeAsync(string shortCode, Guid ownerId, CancellationToken ct)
    {
        return _dbContext.ShortUrls
            .Include(x => x.CustomDomain)
            .FirstOrDefaultAsync(
                x => EF.Functions.Collate(x.ShortCode, CaseSensitiveCollation) == shortCode &&
                    x.OwnerId == ownerId,
                ct);
    }

    public Task<RedirectLookupModel?> GetRedirectAsync(
        RedirectRouteIdentity route,
        CancellationToken ct)
    {
        var query = _dbContext.ShortUrls
            .AsNoTracking()
            .Where(x => EF.Functions.Collate(x.ShortCode, CaseSensitiveCollation) == route.ShortCode);

        query = route.IsDefaultHost
            ? query.Where(x => x.CustomDomainId == null)
            : query.Where(x =>
                x.CustomDomain != null &&
                x.CustomDomain.Status == CustomDomainStatus.Verified &&
                x.CustomDomain.NormalizedHost == route.Host);

        return query
            .Select(x => new RedirectLookupModel(
                x.Id,
                route.Host,
                x.ShortCode,
                x.OriginalUrl,
                x.ExpiresAtUtc,
                x.IsActive,
                x.IsDeleted))
            .FirstOrDefaultAsync(ct);
    }

    public Task<bool> IsRedirectCurrentAsync(
        Guid shortUrlId,
        RedirectRouteIdentity route,
        string expectedOriginalUrl,
        DateTime? expectedExpiresAtUtc,
        DateTime accessedAtUtc,
        CancellationToken ct)
    {
        var query = _dbContext.ShortUrls
            .AsNoTracking()
            .Where(x =>
                x.Id == shortUrlId &&
                EF.Functions.Collate(x.OriginalUrl, BinaryCollation) == expectedOriginalUrl &&
                x.ExpiresAtUtc == expectedExpiresAtUtc &&
                !x.IsDeleted &&
                x.IsActive &&
                (!x.ExpiresAtUtc.HasValue || x.ExpiresAtUtc.Value > accessedAtUtc));

        query = route.IsDefaultHost
            ? query.Where(x => x.CustomDomainId == null)
            : query.Where(x =>
                x.CustomDomain != null &&
                x.CustomDomain.Status == CustomDomainStatus.Verified &&
                x.CustomDomain.NormalizedHost == route.Host);

        return query.AnyAsync(ct);
    }

    public async Task<IReadOnlyList<string>> ListShortCodesForCustomDomainAsync(
        Guid customDomainId,
        CancellationToken ct) =>
        await _dbContext.ShortUrls
            .AsNoTracking()
            .Where(x => x.CustomDomainId == customDomainId)
            .Select(x => x.ShortCode)
            .ToListAsync(ct);

    public async Task<List<(DateTime DateUtc, int Clicks)>> GetDailyClicksAsync(Guid shortUrlId, DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        var firstFullHourUtc = StartOfHour(fromUtc);
        if (firstFullHourUtc < fromUtc)
        {
            firstFullHourUtc = firstFullHourUtc.AddHours(1);
        }

        var endOfFullHoursUtc = StartOfHour(toUtc);
        if (firstFullHourUtc >= endOfFullHoursUtc)
        {
            return await GetRawDailyClicksAsync(shortUrlId, fromUtc, toUtc, ct);
        }

        var aggregateRows = await _dbContext.ShortUrlAnalyticsAggregates
            .AsNoTracking()
            .Where(x =>
                x.ShortUrlId == shortUrlId &&
                x.Granularity == AnalyticsBucketGranularity.Hour &&
                x.Dimension == AnalyticsDimension.Overall &&
                x.DimensionSchemaVersion == AnalyticsDimensionClassifier.SchemaVersion &&
                x.DimensionValue == AnalyticsDimensionClassifier.Overall &&
                x.BucketStartUtc >= firstFullHourUtc &&
                x.BucketStartUtc < endOfFullHoursUtc)
            .GroupBy(x => x.BucketStartUtc.Date)
            .Select(x => new
            {
                DateUtc = x.Key,
                Clicks = x.Sum(row => row.ClickCount)
            })
            .ToListAsync(ct);

        var edgeRows = await _dbContext.ShortUrlAccessLogs
            .AsNoTracking()
            .Where(x =>
                x.ShortUrlId == shortUrlId &&
                x.AccessedAtUtc >= fromUtc &&
                x.AccessedAtUtc <= toUtc &&
                (x.AccessedAtUtc < firstFullHourUtc || x.AccessedAtUtc >= endOfFullHoursUtc))
            .GroupBy(x => x.AccessedAtUtc.Date)
            .Select(x => new
            {
                DateUtc = x.Key,
                Clicks = (long)x.Count()
            })
            .ToListAsync(ct);

        return aggregateRows
            .Concat(edgeRows)
            .GroupBy(x => x.DateUtc)
            .Select(x => (DateUtc: x.Key, Clicks: checked((int)x.Sum(row => row.Clicks))))
            .OrderBy(x => x.DateUtc)
            .ToList();
    }

    public async Task<AnalyticsSummaryReadModel?> GetAnalyticsSummaryAsync(
        AnalyticsSummaryCriteria criteria,
        CancellationToken ct)
    {
        var link = await _dbContext.ShortUrls
            .AsNoTracking()
            .Where(x =>
                x.OwnerId == criteria.OwnerId &&
                !x.IsDeleted &&
                EF.Functions.Collate(x.ShortCode, CaseSensitiveCollation) == criteria.ShortCode)
            .Select(x => new { x.Id, x.ShortCode })
            .FirstOrDefaultAsync(ct);

        if (link is null)
        {
            return null;
        }

        var overallRows = await _dbContext.ShortUrlAnalyticsAggregates
            .AsNoTracking()
            .Where(x =>
                x.ShortUrlId == link.Id &&
                x.Granularity == AnalyticsBucketGranularity.Day &&
                x.Dimension == AnalyticsDimension.Overall &&
                x.DimensionSchemaVersion == AnalyticsDimensionClassifier.SchemaVersion &&
                x.DimensionValue == AnalyticsDimensionClassifier.Overall &&
                x.BucketStartUtc >= criteria.FromUtc &&
                x.BucketStartUtc < criteria.ToUtc)
            .Select(x => new { x.ClickCount, x.UniqueVisitorCount, x.UpdatedAtUtc })
            .ToListAsync(ct);

        var referrers = await GetDimensionCountsAsync(
            link.Id,
            AnalyticsDimension.Referrer,
            criteria.FromUtc,
            criteria.ToUtc,
            criteria.TopReferrers,
            ct);
        var devices = await GetDimensionCountsAsync(
            link.Id,
            AnalyticsDimension.Device,
            criteria.FromUtc,
            criteria.ToUtc,
            limit: null,
            ct);
        var browsers = await GetDimensionCountsAsync(
            link.Id,
            AnalyticsDimension.Browser,
            criteria.FromUtc,
            criteria.ToUtc,
            limit: null,
            ct);
        var operatingSystems = await GetDimensionCountsAsync(
            link.Id,
            AnalyticsDimension.OperatingSystem,
            criteria.FromUtc,
            criteria.ToUtc,
            limit: null,
            ct);

        return new AnalyticsSummaryReadModel(
            link.ShortCode,
            overallRows.Sum(x => x.ClickCount),
            overallRows.Sum(x => x.UniqueVisitorCount),
            overallRows.Count == 0 ? null : overallRows.Max(x => x.UpdatedAtUtc),
            referrers,
            devices,
            browsers,
            operatingSystems);
    }

    public async Task<AnalyticsTimeSeriesReadModel?> GetAnalyticsTimeSeriesAsync(
        AnalyticsTimeSeriesCriteria criteria,
        CancellationToken ct)
    {
        var link = await _dbContext.ShortUrls
            .AsNoTracking()
            .Where(x =>
                x.OwnerId == criteria.OwnerId &&
                !x.IsDeleted &&
                EF.Functions.Collate(x.ShortCode, CaseSensitiveCollation) == criteria.ShortCode)
            .Select(x => new { x.Id, x.ShortCode })
            .FirstOrDefaultAsync(ct);

        if (link is null)
        {
            return null;
        }

        var rows = await _dbContext.ShortUrlAnalyticsAggregates
            .AsNoTracking()
            .Where(x =>
                x.ShortUrlId == link.Id &&
                x.Granularity == criteria.Granularity &&
                x.Dimension == AnalyticsDimension.Overall &&
                x.DimensionSchemaVersion == AnalyticsDimensionClassifier.SchemaVersion &&
                x.DimensionValue == AnalyticsDimensionClassifier.Overall &&
                x.BucketStartUtc >= criteria.FromUtc &&
                x.BucketStartUtc < criteria.ToUtc)
            .OrderBy(x => x.BucketStartUtc)
            .Select(x => new AnalyticsBucketReadModel(x.BucketStartUtc, x.ClickCount, x.UpdatedAtUtc))
            .ToListAsync(ct);

        return new AnalyticsTimeSeriesReadModel(link.ShortCode, rows);
    }

    private async Task<IReadOnlyList<AnalyticsDimensionCount>> GetDimensionCountsAsync(
        Guid shortUrlId,
        AnalyticsDimension dimension,
        DateTime fromUtc,
        DateTime toUtc,
        int? limit,
        CancellationToken ct)
    {
        var query = _dbContext.ShortUrlAnalyticsAggregates
            .AsNoTracking()
            .Where(x =>
                x.ShortUrlId == shortUrlId &&
                x.Granularity == AnalyticsBucketGranularity.Day &&
                x.Dimension == dimension &&
                x.DimensionSchemaVersion == AnalyticsDimensionClassifier.SchemaVersion &&
                x.BucketStartUtc >= fromUtc &&
                x.BucketStartUtc < toUtc)
            .GroupBy(x => x.DimensionValue)
            .Select(group => new { Value = group.Key, Clicks = group.Sum(x => x.ClickCount) })
            .OrderByDescending(x => x.Clicks)
            .ThenBy(x => x.Value);

        var rows = limit.HasValue
            ? await query.Take(limit.Value).ToListAsync(ct)
            : await query.ToListAsync(ct);

        return rows.Select(x => new AnalyticsDimensionCount(x.Value, x.Clicks)).ToList();
    }

    private async Task<List<(DateTime DateUtc, int Clicks)>> GetRawDailyClicksAsync(
        Guid shortUrlId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct)
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

    private static DateTime StartOfHour(DateTime value)
    {
        return new DateTime(value.Year, value.Month, value.Day, value.Hour, 0, 0, value.Kind);
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

    private Task<ShortUrlCreationIdempotencyRecord?> FindIdempotencyRecordAsync(
        ShortUrlIdempotencyContext idempotency,
        CancellationToken ct)
    {
        return _dbContext.ShortUrlCreationIdempotencyRecords
            .AsNoTracking()
            .Include(x => x.ShortUrl)
            .ThenInclude(x => x.CustomDomain)
            .FirstOrDefaultAsync(
                x => x.OwnerId == idempotency.OwnerId &&
                    x.KeyHash == idempotency.KeyHash &&
                    x.ExpiresAtUtc > idempotency.CreatedAtUtc,
                ct);
    }

    private Task<bool> IsCustomDomainAvailableAsync(ShortUrl entity, CancellationToken ct)
    {
        if (!entity.CustomDomainId.HasValue)
        {
            return Task.FromResult(true);
        }

        return _dbContext.CustomDomains
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == entity.CustomDomainId.Value &&
                x.OwnerId == entity.OwnerId &&
                x.Status == CustomDomainStatus.Verified,
                ct);
    }

    private async Task DeleteExpiredIdempotencyRecordsAsync(DateTime nowUtc, CancellationToken ct)
    {
        await _dbContext.ShortUrlCreationIdempotencyRecords
            .Where(x => x.ExpiresAtUtc <= nowUtc)
            .ExecuteDeleteAsync(ct);
    }

    private static IdempotentShortUrlCreationResult ResolveIdempotencyRecord(
        ShortUrlCreationIdempotencyRecord record,
        string requestHash)
    {
        return string.Equals(record.RequestHash, requestHash, StringComparison.Ordinal)
            ? new IdempotentShortUrlCreationResult(
                IdempotentShortUrlCreationOutcome.Existing,
                record.ShortUrl)
            : new IdempotentShortUrlCreationResult(
                IdempotentShortUrlCreationOutcome.RequestConflict);
    }

    private void DetachFailedIdempotentCreation(
        ShortUrl entity,
        ShortUrlCreationIdempotencyRecord record)
    {
        _dbContext.Entry(record).State = EntityState.Detached;
        _dbContext.Entry(entity).State = EntityState.Detached;
    }

    private static bool IsIdempotencyConflict(DbUpdateException exception)
    {
        if (exception.InnerException is not SqlException sqlException)
        {
            return false;
        }

        return sqlException.Errors
            .Cast<SqlError>()
            .Any(error =>
                error.Number is 2601 or 2627 &&
                error.Message.Contains(IdempotencyUniqueIndexName, StringComparison.OrdinalIgnoreCase));
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
