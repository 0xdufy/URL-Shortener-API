using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Application.Interfaces;
using UrlShortener.Application.Messaging;
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

            _dbContext.ShortUrlAccessLogs.Add(new ShortUrlAccessLog
            {
                Id = integrationEvent.EventId,
                ShortUrlId = integrationEvent.Payload.ShortUrlId,
                AccessedAtUtc = accessedAtUtc,
                UserAgent = integrationEvent.Payload.UserAgent,
                Referer = integrationEvent.Payload.ReferrerHost,
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
