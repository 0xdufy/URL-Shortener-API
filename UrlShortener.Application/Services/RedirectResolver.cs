using UrlShortener.Application.Dtos;
using UrlShortener.Application.CustomDomains;
using UrlShortener.Application.Interfaces;
using UrlShortener.Application.Security;

namespace UrlShortener.Application.Services;

public sealed class RedirectResolver : IRedirectResolver
{
    private const int MaximumPersistenceAttempts = 3;

    private readonly IShortUrlRepository _repository;
    private readonly IShortUrlCache _shortUrlCache;
    private readonly IRedirectClickEventPublisher _clickEventPublisher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ShortUrlContractSettings _contractSettings;

    public RedirectResolver(
        IShortUrlRepository repository,
        IShortUrlCache shortUrlCache,
        IRedirectClickEventPublisher clickEventPublisher,
        IDateTimeProvider dateTimeProvider,
        ShortUrlContractSettings contractSettings)
    {
        _repository = repository;
        _shortUrlCache = shortUrlCache;
        _clickEventPublisher = clickEventPublisher;
        _dateTimeProvider = dateTimeProvider;
        _contractSettings = contractSettings;
    }

    public async Task<RedirectResolutionResult> ResolveAsync(
        string effectiveHost,
        string shortCode,
        string ipAddress,
        string? userAgent,
        string? referer,
        CancellationToken ct)
    {
        var route = CreateRoute(effectiveHost, shortCode);
        if (route == null)
        {
            return RedirectResolutionResult.NotFound(RedirectResolutionSource.Persistence);
        }

        var accessedAtUtc = _dateTimeProvider.UtcNow;
        var cachedModel = await _shortUrlCache.GetAsync(route.Host, shortCode, ct);

        if (cachedModel != null)
        {
            var cachedCandidate = CreateCandidate(cachedModel);
            var cachedStatus = EvaluateState(cachedCandidate, accessedAtUtc);
            if (cachedStatus == RedirectResolutionStatus.Resolved &&
                await IsRedirectCurrentAsync(cachedCandidate, route, accessedAtUtc, ct))
            {
                await PublishClickEventAsync(
                    cachedCandidate.ShortUrlId,
                    accessedAtUtc,
                    ipAddress,
                    userAgent,
                    referer,
                    ct);
                return RedirectResolutionResult.Resolved(
                    cachedCandidate.OriginalUrl,
                    RedirectResolutionSource.DistributedCache);
            }

            await _shortUrlCache.RemoveAsync(route.Host, shortCode, ct);
        }

        for (var attempt = 0; attempt < MaximumPersistenceAttempts; attempt++)
        {
            var redirect = await _repository.GetRedirectAsync(route, ct);
            if (redirect == null)
            {
                return RedirectResolutionResult.NotFound(RedirectResolutionSource.Persistence);
            }

            var persistedCandidate = CreateCandidate(redirect);
            var persistedStatus = EvaluateState(persistedCandidate, accessedAtUtc);
            if (persistedStatus == RedirectResolutionStatus.NotFound)
            {
                return RedirectResolutionResult.NotFound(RedirectResolutionSource.Persistence);
            }

            if (persistedStatus == RedirectResolutionStatus.Expired)
            {
                return RedirectResolutionResult.Expired(RedirectResolutionSource.Persistence);
            }

            if (await IsRedirectCurrentAsync(persistedCandidate, route, accessedAtUtc, ct))
            {
                await PublishClickEventAsync(
                    persistedCandidate.ShortUrlId,
                    accessedAtUtc,
                    ipAddress,
                    userAgent,
                    referer,
                    ct);
                await CacheAsync(redirect, accessedAtUtc, ct);
                return RedirectResolutionResult.Resolved(
                    persistedCandidate.OriginalUrl,
                    RedirectResolutionSource.Persistence);
            }

            await _shortUrlCache.RemoveAsync(route.Host, redirect.ShortCode, ct);
        }

        return RedirectResolutionResult.NotFound(RedirectResolutionSource.Persistence);
    }

    private Task<bool> IsRedirectCurrentAsync(
        RedirectCandidate candidate,
        RedirectRouteIdentity route,
        DateTime accessedAtUtc,
        CancellationToken ct)
    {
        return _repository.IsRedirectCurrentAsync(
            candidate.ShortUrlId,
            route,
            candidate.OriginalUrl,
            candidate.ExpiresAtUtc,
            accessedAtUtc,
            ct);
    }

    private Task PublishClickEventAsync(
        Guid shortUrlId,
        DateTime accessedAtUtc,
        string ipAddress,
        string? userAgent,
        string? referer,
        CancellationToken ct)
    {
        var request = new RedirectClickEventRequest(
            shortUrlId,
            new DateTimeOffset(DateTime.SpecifyKind(accessedAtUtc, DateTimeKind.Utc)),
            ipAddress,
            userAgent,
            referer);

        return _clickEventPublisher.PublishBestEffortAsync(request, ct);
    }

    private async Task CacheAsync(
        RedirectLookupModel redirect,
        DateTime nowUtc,
        CancellationToken ct)
    {
        var model = RedirectCachePolicy.CreateModel(redirect);
        var absoluteExpirationUtc = RedirectCachePolicy.CalculateAbsoluteExpiration(model.ExpiresAtUtc, nowUtc);
        if (absoluteExpirationUtc <= nowUtc)
        {
            return;
        }

        await _shortUrlCache.SetAsync(
            redirect.RoutingHost,
            redirect.ShortCode,
            model,
            absoluteExpirationUtc,
            ct);
    }

    private RedirectRouteIdentity? CreateRoute(string effectiveHost, string shortCode)
    {
        if (string.IsNullOrWhiteSpace(effectiveHost) || !ShortUrlInputPolicy.IsValidShortCode(shortCode))
        {
            return null;
        }

        var candidate = effectiveHost.Trim().TrimEnd('.');
        if (candidate.Equals(_contractSettings.DefaultHost, StringComparison.OrdinalIgnoreCase))
        {
            return new RedirectRouteIdentity(_contractSettings.DefaultHost, shortCode, true);
        }

        return CustomDomainHostNormalizer.TryNormalize(candidate, out var normalizedHost, out _)
            ? new RedirectRouteIdentity(normalizedHost, shortCode, false)
            : null;
    }

    private static RedirectResolutionStatus EvaluateState(RedirectCandidate candidate, DateTime nowUtc)
    {
        if (candidate.IsDeleted || !candidate.IsActive || candidate.IsModerationBlocked || candidate.IsOwnerUnavailable)
        {
            return RedirectResolutionStatus.NotFound;
        }

        if (candidate.ExpiresAtUtc.HasValue && candidate.ExpiresAtUtc.Value <= nowUtc)
        {
            return RedirectResolutionStatus.Expired;
        }

        return RedirectResolutionStatus.Resolved;
    }

    private static RedirectCandidate CreateCandidate(ShortUrlCacheModel model) =>
        new(model.ShortUrlId, model.OriginalUrl, model.ExpiresAtUtc, true, false, false, false);

    private static RedirectCandidate CreateCandidate(RedirectLookupModel redirect) =>
        new(
            redirect.ShortUrlId,
            redirect.OriginalUrl,
            redirect.ExpiresAtUtc,
            redirect.IsActive,
            redirect.IsDeleted,
            redirect.IsModerationBlocked,
            redirect.IsOwnerUnavailable);

    private sealed record RedirectCandidate(
        Guid ShortUrlId,
        string OriginalUrl,
        DateTime? ExpiresAtUtc,
        bool IsActive,
        bool IsDeleted,
        bool IsModerationBlocked,
        bool IsOwnerUnavailable);
}
