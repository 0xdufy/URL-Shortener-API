using UrlShortener.Application.Dtos;
using UrlShortener.Application.Exceptions;
using UrlShortener.Application.Interfaces;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Application.Services;

public class ShortUrlService : IShortUrlService
{
    private const int GeneratedShortCodeLength = 8;
    private const int MaxGeneratedShortCodeAttempts = 5;

    private readonly IShortUrlRepository _repository;
    private readonly IShortCodeGenerator _shortCodeGenerator;
    private readonly IShortUrlCache _shortUrlCache;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ShortUrlLifecycleSettings _lifecycleSettings;

    public ShortUrlService(
        IShortUrlRepository repository,
        IShortCodeGenerator shortCodeGenerator,
        IShortUrlCache shortUrlCache,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserContext currentUserContext,
        ShortUrlLifecycleSettings lifecycleSettings)
    {
        _repository = repository;
        _shortCodeGenerator = shortCodeGenerator;
        _shortUrlCache = shortUrlCache;
        _dateTimeProvider = dateTimeProvider;
        _currentUserContext = currentUserContext;
        _lifecycleSettings = lifecycleSettings;
    }

    public async Task<ShortUrlListResponse> ListAsync(ShortUrlListQuery query, CancellationToken ct)
    {
        var criteria = new ShortUrlListCriteria(
            RequireCurrentUserId(),
            query.Page,
            query.PageSize,
            query.Search?.Trim(),
            query.IsActive,
            ParseExpiration(query.Expiration),
            query.IncludeDeleted,
            query.CreatedFromUtc?.UtcDateTime,
            query.CreatedToUtc?.UtcDateTime,
            ParseSortField(query.SortBy),
            query.SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase)
                ? SortDirection.Ascending
                : SortDirection.Descending,
            _dateTimeProvider.UtcNow,
            _lifecycleSettings.RestoreRetentionDays);

        var result = await _repository.ListOwnedAsync(criteria, ct);
        var totalPages = result.TotalItems == 0
            ? 0
            : (int)Math.Ceiling(result.TotalItems / (double)query.PageSize);

        return new ShortUrlListResponse
        {
            Items = result.Items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalItems = result.TotalItems,
            TotalPages = totalPages,
            HasPreviousPage = query.Page > 1,
            HasNextPage = query.Page < totalPages
        };
    }

    public async Task<ShortUrlResponse> CreateAsync(CreateShortUrlRequest req, string baseHost, string clientIp, CancellationToken ct)
    {
        var ownerId = RequireCurrentUserId();
        var nowUtc = _dateTimeProvider.UtcNow;
        ShortUrl entity;

        if (!string.IsNullOrWhiteSpace(req.CustomAlias))
        {
            entity = CreateEntity(req, req.CustomAlias, ownerId, nowUtc);
            var creationResult = await _repository.TryCreateAsync(entity, ct);
            if (creationResult == ShortUrlCreationResult.ShortCodeConflict)
            {
                throw new AliasConflictException("Custom alias already exists.");
            }
        }
        else
        {
            entity = null!;

            for (var attempt = 0; attempt < MaxGeneratedShortCodeAttempts; attempt++)
            {
                var generatedCode = _shortCodeGenerator.Generate(GeneratedShortCodeLength);
                var candidate = CreateEntity(req, generatedCode, ownerId, nowUtc);
                var creationResult = await _repository.TryCreateAsync(candidate, ct);
                if (creationResult == ShortUrlCreationResult.Created)
                {
                    entity = candidate;
                    break;
                }
            }

            if (entity == null)
            {
                throw new ShortCodeGenerationFailedException("Failed to generate unique short code.");
            }
        }

        var cacheModel = new ShortUrlCacheModel
        {
            ShortUrlId = entity.Id,
            OriginalUrl = entity.OriginalUrl,
            ExpiresAtUtc = entity.ExpiresAtUtc,
            IsActive = entity.IsActive,
            IsDeleted = entity.IsDeleted
        };

        await _shortUrlCache.SetAsync(entity.ShortCode, cacheModel, CalculateTtl(entity.ExpiresAtUtc, nowUtc), ct);

        var response = ToShortUrlResponse(entity);
        response.ShortUrl = $"{baseHost}/r/{entity.ShortCode}";

        return response;
    }

    private static ShortUrl CreateEntity(
        CreateShortUrlRequest request,
        string shortCode,
        Guid ownerId,
        DateTime nowUtc)
    {
        return new ShortUrl(ownerId)
        {
            Id = Guid.NewGuid(),
            OriginalUrl = request.OriginalUrl,
            ShortCode = shortCode,
            CreatedAtUtc = nowUtc,
            ExpiresAtUtc = request.ExpiresAtUtc,
            IsActive = true,
            IsDeleted = false,
            ClickCount = 0
        };
    }

    public async Task<ShortUrlDetailsResponse?> GetAsync(string shortCode, CancellationToken ct)
    {
        var ownerId = RequireCurrentUserId();
        var entity = await _repository.GetOwnedByShortCodeNotDeletedAsync(shortCode, ownerId, ct);
        if (entity == null)
        {
            return null;
        }

        return ToShortUrlDetailsResponse(entity);
    }

    public async Task<ShortUrlDetailsResponse?> UpdateAsync(
        string shortCode,
        UpdateShortUrlRequest request,
        CancellationToken ct)
    {
        var ownerId = RequireCurrentUserId();
        var entity = await _repository.GetOwnedByShortCodeNotDeletedAsync(shortCode, ownerId, ct);
        if (entity == null)
        {
            return null;
        }

        entity.OriginalUrl = request.OriginalUrl;
        entity.ExpiresAtUtc = request.ExpiresAtUtc;

        await _repository.SaveChangesAsync(ct);
        await _shortUrlCache.RemoveAsync(shortCode, ct);

        return ToShortUrlDetailsResponse(entity);
    }

    public async Task<ShortUrlDetailsResponse?> SetStatusAsync(string shortCode, bool isActive, CancellationToken ct)
    {
        var ownerId = RequireCurrentUserId();
        var entity = await _repository.GetOwnedByShortCodeNotDeletedAsync(shortCode, ownerId, ct);
        if (entity == null)
        {
            return null;
        }

        entity.IsActive = isActive;

        await _repository.SaveChangesAsync(ct);
        await _shortUrlCache.RemoveAsync(shortCode, ct);

        return ToShortUrlDetailsResponse(entity);
    }

    public async Task<bool> DeleteAsync(string shortCode, CancellationToken ct)
    {
        var ownerId = RequireCurrentUserId();
        var entity = await _repository.GetOwnedByShortCodeNotDeletedAsync(shortCode, ownerId, ct);
        if (entity == null)
        {
            return false;
        }

        entity.IsDeleted = true;
        entity.DeletedAtUtc = _dateTimeProvider.UtcNow;

        await _repository.SaveChangesAsync(ct);
        await _shortUrlCache.RemoveAsync(shortCode, ct);

        return true;
    }

    public async Task<ShortUrlDetailsResponse> RestoreAsync(string shortCode, CancellationToken ct)
    {
        var ownerId = RequireCurrentUserId();
        var entity = await _repository.GetOwnedByShortCodeAsync(shortCode, ownerId, ct);
        if (entity == null)
        {
            throw new NotFoundException("Short URL not found.");
        }

        if (!entity.IsDeleted)
        {
            throw new RestoreNotDeletedException("Short URL is not deleted.");
        }

        if (!entity.DeletedAtUtc.HasValue ||
            _dateTimeProvider.UtcNow >= entity.DeletedAtUtc.Value.AddDays(_lifecycleSettings.RestoreRetentionDays))
        {
            throw new RestoreWindowExpiredException("The restore window has expired.");
        }

        entity.IsDeleted = false;
        entity.DeletedAtUtc = null;

        await _repository.SaveChangesAsync(ct);
        await _shortUrlCache.RemoveAsync(shortCode, ct);

        return ToShortUrlDetailsResponse(entity);
    }

    public async Task<StatsResponse?> GetStatsAsync(string shortCode, DateTime? fromUtc, DateTime? toUtc, CancellationToken ct)
    {
        var ownerId = RequireCurrentUserId();
        var entity = await _repository.GetOwnedByShortCodeNotDeletedAsync(shortCode, ownerId, ct);
        if (entity == null)
        {
            return null;
        }

        var nowUtc = _dateTimeProvider.UtcNow;
        var actualToUtc = toUtc ?? nowUtc;
        var actualFromUtc = fromUtc ?? nowUtc.AddDays(-30);

        var dailyClickCounts = await _repository.GetDailyClicksAsync(entity.Id, actualFromUtc, actualToUtc, ct);
        var dailyClicks = dailyClickCounts
            .Select(x => new DailyClicksItem
            {
                DateUtc = x.DateUtc.ToString("yyyy-MM-dd"),
                Clicks = x.Clicks
            })
            .ToList();

        return new StatsResponse
        {
            ShortCode = entity.ShortCode,
            TotalClicks = dailyClickCounts.Sum(x => (long)x.Clicks),
            FromUtc = actualFromUtc,
            ToUtc = actualToUtc,
            DailyClicks = dailyClicks
        };
    }

    public async Task<(int statusCode, string? originalUrl)> ResolveForRedirectAsync(string shortCode, string ip, string? userAgent, string? referer, CancellationToken ct)
    {
        var nowUtc = _dateTimeProvider.UtcNow;
        var cacheModel = await _shortUrlCache.GetAsync(shortCode, ct);

        if (cacheModel != null)
        {
            if (cacheModel.IsDeleted || !cacheModel.IsActive)
            {
                return (404, null);
            }

            if (cacheModel.ExpiresAtUtc.HasValue && cacheModel.ExpiresAtUtc.Value <= nowUtc)
            {
                return (410, null);
            }

            var registeredFromCache = await RegisterAccessAsync(cacheModel.ShortUrlId, nowUtc, ip, userAgent, referer, ct);
            if (registeredFromCache)
            {
                return (302, cacheModel.OriginalUrl);
            }

            await _shortUrlCache.RemoveAsync(shortCode, ct);
        }

        var entity = await _repository.GetByShortCodeAnyAsync(shortCode, ct);
        if (entity == null || entity.IsDeleted)
        {
            return (404, null);
        }

        if (!entity.IsActive)
        {
            return (404, null);
        }

        if (entity.ExpiresAtUtc.HasValue && entity.ExpiresAtUtc.Value <= nowUtc)
        {
            return (410, null);
        }

        var model = new ShortUrlCacheModel
        {
            ShortUrlId = entity.Id,
            OriginalUrl = entity.OriginalUrl,
            ExpiresAtUtc = entity.ExpiresAtUtc,
            IsActive = entity.IsActive,
            IsDeleted = entity.IsDeleted
        };

        await _shortUrlCache.SetAsync(entity.ShortCode, model, CalculateTtl(entity.ExpiresAtUtc, nowUtc), ct);

        var registered = await RegisterAccessAsync(entity.Id, nowUtc, ip, userAgent, referer, ct);
        if (!registered)
        {
            return (404, null);
        }

        return (302, entity.OriginalUrl);
    }

    private async Task<bool> RegisterAccessAsync(Guid shortUrlId, DateTime nowUtc, string ip, string? userAgent, string? referer, CancellationToken ct)
    {
        var updated = await _repository.IncrementClickCountAsync(shortUrlId, nowUtc, ct);
        if (!updated)
        {
            return false;
        }

        var log = new ShortUrlAccessLog
        {
            Id = Guid.NewGuid(),
            ShortUrlId = shortUrlId,
            AccessedAtUtc = nowUtc,
            IpAddress = ip,
            UserAgent = userAgent,
            Referer = referer
        };

        await _repository.AddAccessLogAsync(log, ct);
        await _repository.SaveChangesAsync(ct);

        return true;
    }

    private Guid RequireCurrentUserId()
    {
        var userId = _currentUserContext.UserId;
        if (!userId.HasValue || userId.Value == Guid.Empty)
        {
            throw new AuthenticatedUserRequiredException();
        }

        return userId.Value;
    }

    private static ShortUrlExpirationFilter ParseExpiration(string value) =>
        value.ToLowerInvariant() switch
        {
            "expired" => ShortUrlExpirationFilter.Expired,
            "notexpired" => ShortUrlExpirationFilter.NotExpired,
            _ => ShortUrlExpirationFilter.All
        };

    private static ShortUrlSortField ParseSortField(string value) =>
        value.ToLowerInvariant() switch
        {
            "shortcode" => ShortUrlSortField.ShortCode,
            "clickcount" => ShortUrlSortField.ClickCount,
            "expiresat" => ShortUrlSortField.ExpiresAt,
            _ => ShortUrlSortField.CreatedAt
        };

    private static TimeSpan CalculateTtl(DateTime? expiresAtUtc, DateTime nowUtc)
    {
        if (expiresAtUtc.HasValue)
        {
            var ttl = expiresAtUtc.Value - nowUtc;
            if (ttl < TimeSpan.FromMinutes(1))
            {
                return TimeSpan.FromMinutes(1);
            }

            return ttl;
        }

        return TimeSpan.FromHours(24);
    }

    private static ShortUrlResponse ToShortUrlResponse(ShortUrl entity)
    {
        return new ShortUrlResponse
        {
            Id = entity.Id,
            OriginalUrl = entity.OriginalUrl,
            ShortCode = entity.ShortCode,
            CreatedAtUtc = entity.CreatedAtUtc,
            ExpiresAtUtc = entity.ExpiresAtUtc,
            IsActive = entity.IsActive,
            ClickCount = entity.ClickCount
        };
    }

    private ShortUrlDetailsResponse ToShortUrlDetailsResponse(ShortUrl entity)
    {
        return new ShortUrlDetailsResponse
        {
            Id = entity.Id,
            OriginalUrl = entity.OriginalUrl,
            ShortCode = entity.ShortCode,
            CreatedAtUtc = entity.CreatedAtUtc,
            ExpiresAtUtc = entity.ExpiresAtUtc,
            IsActive = entity.IsActive,
            IsDeleted = entity.IsDeleted,
            DeletedAtUtc = entity.DeletedAtUtc,
            RestoreUntilUtc = entity.DeletedAtUtc?.AddDays(_lifecycleSettings.RestoreRetentionDays),
            ClickCount = entity.ClickCount,
            LastAccessedAtUtc = entity.LastAccessedAtUtc
        };
    }
}
