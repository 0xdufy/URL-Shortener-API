using UrlShortener.Application.Dtos;
using UrlShortener.Application.Exceptions;
using UrlShortener.Application.Interfaces;
using UrlShortener.Domain.Entities;
using UrlShortener.Domain.Moderation;

namespace UrlShortener.Application.Services;

public sealed class ShortUrlModerationService : IShortUrlModerationService
{
    private readonly IShortUrlRepository _repository;
    private readonly IShortUrlCache _cache;
    private readonly ICurrentUserContext _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ShortUrlContractSettings _contractSettings;

    public ShortUrlModerationService(
        IShortUrlRepository repository,
        IShortUrlCache cache,
        ICurrentUserContext currentUser,
        IDateTimeProvider clock,
        ShortUrlContractSettings contractSettings)
    {
        _repository = repository;
        _cache = cache;
        _currentUser = currentUser;
        _clock = clock;
        _contractSettings = contractSettings;
    }

    public async Task<ShortUrlModerationResponse?> ModerateAsync(
        Guid shortUrlId,
        ModerateShortUrlRequest request,
        CancellationToken ct)
    {
        var actorUserId = _currentUser.UserId;
        if (!actorUserId.HasValue || actorUserId.Value == Guid.Empty)
        {
            throw new AuthenticatedUserRequiredException();
        }

        var shortUrl = await _repository.GetByIdAsync(shortUrlId, ct);
        if (shortUrl is null)
        {
            return null;
        }

        var nowUtc = _clock.UtcNow;
        var status = request.IsBlocked
            ? ShortUrlModerationStatus.Blocked
            : ShortUrlModerationStatus.Cleared;
        shortUrl.ApplyModeration(status, request.PublicReasonCode, actorUserId.Value, nowUtc);

        var auditAction = new ShortUrlModerationAction(
            shortUrl.Id,
            actorUserId.Value,
            request.IsBlocked ? "blocked" : "cleared",
            request.PublicReasonCode,
            request.InternalReason,
            nowUtc);
        await _repository.AddModerationActionAsync(auditAction, ct);
        await _repository.SaveChangesAsync(ct);

        var routingHost = shortUrl.CustomDomain?.NormalizedHost ?? _contractSettings.DefaultHost;
        await _cache.RemoveAsync(routingHost, shortUrl.ShortCode, ct);

        return new ShortUrlModerationResponse(
            shortUrl.Id,
            status.ToString().ToLowerInvariant(),
            shortUrl.ModerationPublicReasonCode,
            nowUtc);
    }
}
