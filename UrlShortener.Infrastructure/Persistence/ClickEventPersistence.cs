using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Application.Interfaces;
using UrlShortener.Application.Messaging;
using UrlShortener.Domain.Analytics;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Infrastructure.Persistence;

public sealed class ClickEventPersistence : IClickEventPersistence
{
    private const string AccessLogPrimaryKeyName = "PK_ShortUrlAccessLogs";

    private readonly AppDbContext _dbContext;

    public ClickEventPersistence(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ClickEventPersistenceOutcome> PersistAsync(
        IntegrationEvent<ClickEventV1> integrationEvent,
        CancellationToken cancellationToken = default)
    {
        if (await _dbContext.ShortUrlAccessLogs
            .AsNoTracking()
            .AnyAsync(x => x.Id == integrationEvent.EventId, cancellationToken))
        {
            return ClickEventPersistenceOutcome.Duplicate;
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var accessedAtUtc = integrationEvent.Payload.AccessedAtUtc.UtcDateTime;

        try
        {
            var updatedRows = await _dbContext.ShortUrls
                .Where(x => x.Id == integrationEvent.Payload.ShortUrlId)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(x => x.ClickCount, x => x.ClickCount + 1)
                    .SetProperty(
                        x => x.LastAccessedAtUtc,
                        x => !x.LastAccessedAtUtc.HasValue || x.LastAccessedAtUtc < accessedAtUtc
                            ? accessedAtUtc
                            : x.LastAccessedAtUtc),
                    cancellationToken);

            if (updatedRows == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return ClickEventPersistenceOutcome.ShortUrlNotFound;
            }

            var dimensions = AnalyticsDimensionClassifier.Classify(
                integrationEvent.Payload.ReferrerHost,
                integrationEvent.Payload.ReferrerKind,
                integrationEvent.Payload.UserAgent);
            var hourlyBucketStartUtc = StartOfHour(accessedAtUtc);
            var dailyBucketStartUtc = accessedAtUtc.Date;
            var uniqueVisitorIncrement = await InsertDailyVisitorIfNewAsync(
                integrationEvent.Payload,
                accessedAtUtc,
                cancellationToken);

            await IncrementAggregateAsync(
                integrationEvent.Payload.ShortUrlId,
                hourlyBucketStartUtc,
                AnalyticsBucketGranularity.Hour,
                AnalyticsDimension.Overall,
                AnalyticsDimensionClassifier.Overall,
                uniqueVisitorIncrement: 0,
                cancellationToken);
            await IncrementAggregateAsync(
                integrationEvent.Payload.ShortUrlId,
                dailyBucketStartUtc,
                AnalyticsBucketGranularity.Day,
                AnalyticsDimension.Overall,
                AnalyticsDimensionClassifier.Overall,
                uniqueVisitorIncrement,
                cancellationToken);
            await IncrementAggregateAsync(
                integrationEvent.Payload.ShortUrlId,
                dailyBucketStartUtc,
                AnalyticsBucketGranularity.Day,
                AnalyticsDimension.Referrer,
                dimensions.Referrer,
                uniqueVisitorIncrement: 0,
                cancellationToken);
            await IncrementAggregateAsync(
                integrationEvent.Payload.ShortUrlId,
                dailyBucketStartUtc,
                AnalyticsBucketGranularity.Day,
                AnalyticsDimension.Device,
                dimensions.Device,
                uniqueVisitorIncrement: 0,
                cancellationToken);
            await IncrementAggregateAsync(
                integrationEvent.Payload.ShortUrlId,
                dailyBucketStartUtc,
                AnalyticsBucketGranularity.Day,
                AnalyticsDimension.Browser,
                dimensions.Browser,
                uniqueVisitorIncrement: 0,
                cancellationToken);
            await IncrementAggregateAsync(
                integrationEvent.Payload.ShortUrlId,
                dailyBucketStartUtc,
                AnalyticsBucketGranularity.Day,
                AnalyticsDimension.OperatingSystem,
                dimensions.OperatingSystem,
                uniqueVisitorIncrement: 0,
                cancellationToken);

            _dbContext.ShortUrlAccessLogs.Add(new ShortUrlAccessLog
            {
                Id = integrationEvent.EventId,
                ShortUrlId = integrationEvent.Payload.ShortUrlId,
                AccessedAtUtc = accessedAtUtc,
                UserAgent = integrationEvent.Payload.UserAgent,
                Referer = integrationEvent.Payload.ReferrerHost,
                ReferrerKind = integrationEvent.Payload.ReferrerKind,
                PseudonymousVisitorId = integrationEvent.Payload.PseudonymousVisitorId,
                VisitorIdentityPeriodUtc = integrationEvent.Payload.VisitorIdentityPeriodUtc,
                VisitorIdentityScheme = integrationEvent.Payload.VisitorIdentityScheme
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ClickEventPersistenceOutcome.Persisted;
        }
        catch (DbUpdateException exception) when (IsDuplicateEvent(exception))
        {
            await transaction.RollbackAsync(CancellationToken.None);
            _dbContext.ChangeTracker.Clear();

            var persisted = await _dbContext.ShortUrlAccessLogs
                .AsNoTracking()
                .AnyAsync(x => x.Id == integrationEvent.EventId, cancellationToken);
            if (!persisted)
            {
                throw;
            }

            return ClickEventPersistenceOutcome.Duplicate;
        }
    }

    private async Task<int> InsertDailyVisitorIfNewAsync(
        ClickEventV1 payload,
        DateTime accessedAtUtc,
        CancellationToken cancellationToken)
    {
        var insertedRows = await _dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [ShortUrlAnalyticsDailyVisitors]
                ([ShortUrlId], [IdentityPeriodUtc], [PseudonymousVisitorId], [IdentityScheme], [FirstSeenAtUtc])
            SELECT
                {payload.ShortUrlId},
                {payload.VisitorIdentityPeriodUtc},
                CONVERT(varchar(64), {payload.PseudonymousVisitorId}),
                CONVERT(varchar(64), {payload.VisitorIdentityScheme}),
                {accessedAtUtc}
            WHERE NOT EXISTS
            (
                SELECT 1
                FROM [ShortUrlAnalyticsDailyVisitors] WITH (UPDLOCK, HOLDLOCK)
                WHERE [ShortUrlId] = {payload.ShortUrlId}
                  AND [IdentityPeriodUtc] = {payload.VisitorIdentityPeriodUtc}
                  AND [PseudonymousVisitorId] = CONVERT(varchar(64), {payload.PseudonymousVisitorId})
            );
            """, cancellationToken);

        return insertedRows == 1 ? 1 : 0;
    }

    private Task IncrementAggregateAsync(
        Guid shortUrlId,
        DateTime bucketStartUtc,
        AnalyticsBucketGranularity granularity,
        AnalyticsDimension dimension,
        string dimensionValue,
        int uniqueVisitorIncrement,
        CancellationToken cancellationToken)
    {
        var granularityValue = (byte)granularity;
        var dimensionValueCode = (byte)dimension;

        return _dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE [ShortUrlAnalyticsAggregates] WITH (UPDLOCK, SERIALIZABLE)
            SET [ClickCount] = [ClickCount] + 1,
                [UniqueVisitorCount] = [UniqueVisitorCount] + {uniqueVisitorIncrement},
                [UpdatedAtUtc] = SYSUTCDATETIME()
            WHERE [ShortUrlId] = {shortUrlId}
              AND [Granularity] = {granularityValue}
              AND [Dimension] = {dimensionValueCode}
              AND [DimensionSchemaVersion] = {AnalyticsDimensionClassifier.SchemaVersion}
              AND [BucketStartUtc] = {bucketStartUtc}
              AND [DimensionValue] = CONVERT(varchar(253), {dimensionValue});

            IF @@ROWCOUNT = 0
            BEGIN
                INSERT INTO [ShortUrlAnalyticsAggregates]
                    ([ShortUrlId], [BucketStartUtc], [Granularity], [Dimension],
                     [DimensionSchemaVersion], [DimensionValue], [ClickCount],
                     [UniqueVisitorCount], [UpdatedAtUtc])
                VALUES
                    ({shortUrlId}, {bucketStartUtc}, {granularityValue}, {dimensionValueCode},
                     {AnalyticsDimensionClassifier.SchemaVersion}, CONVERT(varchar(253), {dimensionValue}), 1,
                     {uniqueVisitorIncrement}, SYSUTCDATETIME());
            END;
            """, cancellationToken);
    }

    private static DateTime StartOfHour(DateTime value)
    {
        return new DateTime(value.Year, value.Month, value.Day, value.Hour, 0, 0, DateTimeKind.Utc);
    }

    private static bool IsDuplicateEvent(DbUpdateException exception)
    {
        if (exception.InnerException is not SqlException sqlException)
        {
            return false;
        }

        return sqlException.Errors
            .Cast<SqlError>()
            .Any(error =>
                error.Number is 2601 or 2627 &&
                error.Message.Contains(AccessLogPrimaryKeyName, StringComparison.OrdinalIgnoreCase));
    }
}
