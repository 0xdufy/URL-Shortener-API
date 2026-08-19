using UrlShortener.Application.Interfaces;

namespace UrlShortener.Application.Services;

public sealed class UnavailableApiKeyAuthenticationRepository : IApiKeyAuthenticationRepository
{
    public Task<ApiKeyAuthenticationRecord?> FindByPrefixAsync(string keyPrefix, CancellationToken ct) =>
        Task.FromResult<ApiKeyAuthenticationRecord?>(null);

    public Task RecordUseIfStaleAsync(
        Guid apiKeyId,
        DateTime usedAtUtc,
        TimeSpan minimumWriteInterval,
        CancellationToken ct) => Task.CompletedTask;
}
