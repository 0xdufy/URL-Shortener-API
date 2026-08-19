using System.Data;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Application.Interfaces;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Infrastructure.Persistence.Repositories;

public sealed class ApiKeyRepository : IApiKeyRepository
{
    private readonly AppDbContext _dbContext;

    public ApiKeyRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ApiKey>> ListOwnedAsync(Guid ownerId, CancellationToken ct)
    {
        return await _dbContext.ApiKeys
            .AsNoTracking()
            .Where(apiKey => apiKey.OwnerId == ownerId)
            .OrderByDescending(apiKey => apiKey.CreatedAtUtc)
            .ThenByDescending(apiKey => apiKey.Id)
            .ToListAsync(ct);
    }

    public Task<ApiKey?> GetOwnedAsync(Guid apiKeyId, Guid ownerId, CancellationToken ct)
    {
        return _dbContext.ApiKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(apiKey => apiKey.Id == apiKeyId && apiKey.OwnerId == ownerId, ct);
    }

    public async Task<ApiKeyCreationOutcome> TryCreateAsync(
        ApiKey apiKey,
        DateTime utcNow,
        int maximumActiveKeys,
        CancellationToken ct)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var activeCount = await _dbContext.ApiKeys.CountAsync(
            candidate => candidate.OwnerId == apiKey.OwnerId &&
                candidate.RevokedAtUtc == null &&
                (candidate.ExpiresAtUtc == null || candidate.ExpiresAtUtc > utcNow),
            ct);

        if (activeCount >= maximumActiveKeys)
        {
            return ApiKeyCreationOutcome.ActiveKeyLimitReached;
        }

        _dbContext.ApiKeys.Add(apiKey);
        await _dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return ApiKeyCreationOutcome.Created;
    }

    public async Task<ApiKeyRevocationOutcome> TryRevokeAsync(
        Guid apiKeyId,
        Guid ownerId,
        DateTime revokedAtUtc,
        CancellationToken ct)
    {
        var apiKey = await _dbContext.ApiKeys
            .FirstOrDefaultAsync(candidate => candidate.Id == apiKeyId && candidate.OwnerId == ownerId, ct);
        if (apiKey == null)
        {
            return ApiKeyRevocationOutcome.NotFound;
        }

        if (apiKey.RevokedAtUtc.HasValue)
        {
            return ApiKeyRevocationOutcome.AlreadyRevoked;
        }

        apiKey.Revoke(revokedAtUtc, "user_revoked");
        await _dbContext.SaveChangesAsync(ct);
        return ApiKeyRevocationOutcome.Revoked;
    }

    public async Task<ApiKeyRotationOutcome> TryRotateAsync(
        Guid apiKeyId,
        Guid ownerId,
        ApiKey replacement,
        DateTime rotatedAtUtc,
        CancellationToken ct)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var existing = await _dbContext.ApiKeys
            .FirstOrDefaultAsync(candidate => candidate.Id == apiKeyId && candidate.OwnerId == ownerId, ct);
        if (existing == null)
        {
            return ApiKeyRotationOutcome.NotFound;
        }

        if (!existing.IsActiveAt(rotatedAtUtc))
        {
            return ApiKeyRotationOutcome.NotActive;
        }

        _dbContext.ApiKeys.Add(replacement);
        existing.Revoke(rotatedAtUtc, "rotated", replacement.Id);
        await _dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return ApiKeyRotationOutcome.Rotated;
    }
}
