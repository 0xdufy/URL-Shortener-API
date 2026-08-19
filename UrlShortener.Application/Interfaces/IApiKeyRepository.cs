using UrlShortener.Domain.Entities;

namespace UrlShortener.Application.Interfaces;

public interface IApiKeyRepository
{
    Task<IReadOnlyList<ApiKey>> ListOwnedAsync(Guid ownerId, CancellationToken ct);
    Task<ApiKey?> GetOwnedAsync(Guid apiKeyId, Guid ownerId, CancellationToken ct);
    Task<ApiKeyCreationOutcome> TryCreateAsync(
        ApiKey apiKey,
        DateTime utcNow,
        int maximumActiveKeys,
        CancellationToken ct);
    Task<ApiKeyRevocationOutcome> TryRevokeAsync(
        Guid apiKeyId,
        Guid ownerId,
        DateTime revokedAtUtc,
        CancellationToken ct);
    Task<ApiKeyRotationOutcome> TryRotateAsync(
        Guid apiKeyId,
        Guid ownerId,
        ApiKey replacement,
        DateTime rotatedAtUtc,
        CancellationToken ct);
}

public enum ApiKeyCreationOutcome
{
    Created,
    ActiveKeyLimitReached
}

public enum ApiKeyRevocationOutcome
{
    Revoked,
    NotFound,
    AlreadyRevoked
}

public enum ApiKeyRotationOutcome
{
    Rotated,
    NotFound,
    NotActive
}
