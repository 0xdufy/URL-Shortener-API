using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Application.Interfaces;
using UrlShortener.Domain.CustomDomains;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Infrastructure.Persistence.Repositories;

public sealed class CustomDomainRepository : ICustomDomainRepository
{
    private readonly AppDbContext _dbContext;

    public CustomDomainRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CustomDomainCreateOutcome> TryCreateAsync(CustomDomain customDomain, CancellationToken ct)
    {
        _dbContext.CustomDomains.Add(customDomain);
        try
        {
            await _dbContext.SaveChangesAsync(ct);
            return CustomDomainCreateOutcome.Created;
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            _dbContext.Entry(customDomain).State = EntityState.Detached;
            return CustomDomainCreateOutcome.HostConflict;
        }
    }

    public async Task<IReadOnlyList<CustomDomain>> ListOwnedAsync(Guid ownerId, CancellationToken ct) =>
        await _dbContext.CustomDomains
            .AsNoTracking()
            .Where(customDomain => customDomain.OwnerId == ownerId)
            .OrderByDescending(customDomain => customDomain.CreatedAtUtc)
            .ThenByDescending(customDomain => customDomain.Id)
            .ToListAsync(ct);

    public Task<CustomDomain?> GetOwnedAsync(Guid customDomainId, Guid ownerId, CancellationToken ct) =>
        _dbContext.CustomDomains
            .FirstOrDefaultAsync(
                customDomain => customDomain.Id == customDomainId && customDomain.OwnerId == ownerId,
                ct);

    public async Task<CustomDomainMutationOutcome> RequestVerificationAsync(
        Guid customDomainId,
        Guid ownerId,
        string verificationToken,
        DateTime requestedAtUtc,
        CancellationToken ct)
    {
        var customDomain = await FindOwnedForUpdateAsync(customDomainId, ownerId, ct);
        if (customDomain == null)
        {
            return CustomDomainMutationOutcome.NotFound;
        }

        customDomain.RequestVerification(verificationToken, requestedAtUtc);
        return await SaveMutationAsync(ct);
    }

    public async Task<CustomDomainMutationOutcome> RecordVerificationAsync(
        Guid customDomainId,
        Guid ownerId,
        string expectedVerificationToken,
        CustomDomainVerificationEvidence evidence,
        DateTime attemptedAtUtc,
        CancellationToken ct)
    {
        var customDomain = await FindOwnedForUpdateAsync(customDomainId, ownerId, ct);
        if (customDomain == null)
        {
            return CustomDomainMutationOutcome.NotFound;
        }

        if (!string.Equals(customDomain.VerificationToken, expectedVerificationToken, StringComparison.Ordinal) ||
            customDomain.Status is CustomDomainStatus.Disabled or CustomDomainStatus.Verified)
        {
            return CustomDomainMutationOutcome.StateChanged;
        }

        if (evidence.Status == CustomDomainVerificationEvidenceStatus.Verified)
        {
            customDomain.RecordVerificationSuccess(attemptedAtUtc);
        }
        else
        {
            customDomain.RecordVerificationFailure(
                attemptedAtUtc,
                evidence.FailureCode ?? "DNS_VERIFICATION_FAILED",
                evidence.FailureMessage ?? "DNS ownership verification failed. Review the TXT record and try again.");
        }

        return await SaveMutationAsync(ct);
    }

    public async Task<CustomDomainMutationOutcome> DisableAsync(
        Guid customDomainId,
        Guid ownerId,
        DateTime disabledAtUtc,
        CancellationToken ct)
    {
        var customDomain = await FindOwnedForUpdateAsync(customDomainId, ownerId, ct);
        if (customDomain == null)
        {
            return CustomDomainMutationOutcome.NotFound;
        }

        customDomain.Disable(disabledAtUtc);
        return await SaveMutationAsync(ct);
    }

    private Task<CustomDomain?> FindOwnedForUpdateAsync(Guid customDomainId, Guid ownerId, CancellationToken ct) =>
        _dbContext.CustomDomains.FirstOrDefaultAsync(
            customDomain => customDomain.Id == customDomainId && customDomain.OwnerId == ownerId,
            ct);

    private async Task<CustomDomainMutationOutcome> SaveMutationAsync(CancellationToken ct)
    {
        try
        {
            await _dbContext.SaveChangesAsync(ct);
            return CustomDomainMutationOutcome.Updated;
        }
        catch (DbUpdateConcurrencyException)
        {
            _dbContext.ChangeTracker.Clear();
            return CustomDomainMutationOutcome.StateChanged;
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
