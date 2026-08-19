using UrlShortener.Domain.ApiKeys;
using UrlShortener.Domain.Identity;

namespace UrlShortener.Application.Interfaces;

public interface IApiKeyAuthenticationRepository
{
    Task<ApiKeyAuthenticationRecord?> FindByPrefixAsync(string keyPrefix, CancellationToken ct);
    Task RecordUseIfStaleAsync(
        Guid apiKeyId,
        DateTime usedAtUtc,
        TimeSpan minimumWriteInterval,
        CancellationToken ct);
}

public sealed record ApiKeyAuthenticationRecord(
    Guid Id,
    Guid OwnerId,
    byte[] SecretHash,
    ApiKeyScope Scopes,
    DateTime? ExpiresAtUtc,
    DateTime? RevokedAtUtc,
    DateTime? LastUsedAtUtc,
    UserAccountStatus OwnerStatus);
