using UrlShortener.Application.Dtos;
using UrlShortener.Application.Interfaces;

namespace UrlShortener.Application.Services;

public sealed class RedirectResolver : IRedirectResolver
{
    private const int MaximumPersistenceAttempts = 3;

    private readonly IShortUrlRepository _repository;
    private readonly IShortUrlCache _shortUrlCache;
    private readonly IRedirectAccessRecorder _accessRecorder;
    private readonly IRedirectClickEventPublisher _clickEventPublisher;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RedirectResolver(
        IShortUrlRepository repository,
        IShortUrlCache shortUrlCache,
        IRedirectAccessRecorder accessRecorder,
        IRedirectClickEventPublisher clickEventPublisher,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _shortUrlCache = shortUrlCache;
        _accessRecorder = accessRecorder;
        _clickEventPublisher = clickEventPublisher;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<RedirectResolutionResult> ResolveAsync(
        string shortCode,
        string ipAddress,
        string? userAgent,
        string? referer,
        CancellationToken ct)
    {
        var accessedAtUtc = _dateTimeProvider.UtcNow;
        var cachedModel = await _shortUrlCache.GetAsync(shortCode, ct);

        if (cachedModel != null)
        {
            var cachedCandidate = CreateCandidate(cachedModel);
            var cachedStatus = EvaluateState(cachedCandidate, accessedAtUtc);
            if (cachedStatus == RedirectResolutionStatus.Resolved &&
                await TryRecordAccessAsync(cachedCandidate, accessedAtUtc, ipAddress, userAgent, referer, ct))
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

            await _shortUrlCache.RemoveAsync(shortCode, ct);
        }

        for (var attempt = 0; attempt < MaximumPersistenceAttempts; attempt++)
        {
            var redirect = await _repository.GetRedirectByShortCodeAsync(shortCode, ct);
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

            if (await TryRecordAccessAsync(
                persistedCandidate,
                accessedAtUtc,
                ipAddress,
                userAgent,
                referer,
                ct))
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

            await _shortUrlCache.RemoveAsync(redirect.ShortCode, ct);
        }

        return RedirectResolutionResult.NotFound(RedirectResolutionSource.Persistence);
    }

    private async Task<bool> TryRecordAccessAsync(
        RedirectCandidate candidate,
        DateTime accessedAtUtc,
        string ipAddress,
        string? userAgent,
        string? referer,
        CancellationToken ct)
    {
        var request = new RedirectAccessRequest(
            candidate.ShortUrlId,
            candidate.OriginalUrl,
            candidate.ExpiresAtUtc,
            accessedAtUtc,
            ipAddress,
            userAgent,
            referer);

        return await _accessRecorder.TryRecordAsync(request, ct);
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

        await _shortUrlCache.SetAsync(redirect.ShortCode, model, absoluteExpirationUtc, ct);
    }

    private static RedirectResolutionStatus EvaluateState(RedirectCandidate candidate, DateTime nowUtc)
    {
        if (candidate.IsDeleted || !candidate.IsActive)
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
        new(model.ShortUrlId, model.OriginalUrl, model.ExpiresAtUtc, true, false);

    private static RedirectCandidate CreateCandidate(RedirectLookupModel redirect) =>
        new(
            redirect.ShortUrlId,
            redirect.OriginalUrl,
            redirect.ExpiresAtUtc,
            redirect.IsActive,
            redirect.IsDeleted);

    private sealed record RedirectCandidate(
        Guid ShortUrlId,
        string OriginalUrl,
        DateTime? ExpiresAtUtc,
        bool IsActive,
        bool IsDeleted);
}
