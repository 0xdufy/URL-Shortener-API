using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UrlShortener.Application.Dtos;
using UrlShortener.Application.Exceptions;
using UrlShortener.Application.Interfaces;
using UrlShortener.Application.Security;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Application.Services;

public class ShortUrlService : IShortUrlService
{
    private const int GeneratedShortCodeLength = 8;
    private const int MaxGeneratedShortCodeAttempts = 5;

    private readonly IShortUrlRepository _repository;
    private readonly IShortCodeGenerator _shortCodeGenerator;
    private readonly IShortUrlCache _shortUrlCache;
    private readonly ICustomDomainRepository _customDomainRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ShortUrlLifecycleSettings _lifecycleSettings;
    private readonly ShortUrlContractSettings _contractSettings;
    private readonly IdempotencySettings _idempotencySettings;

    public ShortUrlService(
        IShortUrlRepository repository,
        IShortCodeGenerator shortCodeGenerator,
        IShortUrlCache shortUrlCache,
        ICustomDomainRepository customDomainRepository,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserContext currentUserContext,
        ShortUrlLifecycleSettings lifecycleSettings,
        ShortUrlContractSettings contractSettings,
        IdempotencySettings idempotencySettings)
    {
        _repository = repository;
        _shortCodeGenerator = shortCodeGenerator;
        _shortUrlCache = shortUrlCache;
        _customDomainRepository = customDomainRepository;
        _dateTimeProvider = dateTimeProvider;
        _currentUserContext = currentUserContext;
        _lifecycleSettings = lifecycleSettings;
        _contractSettings = contractSettings;
        _idempotencySettings = idempotencySettings;
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
        foreach (var item in result.Items)
        {
            item.ShortUrl = BuildPublicShortUrl(item.ShortCode, item.CustomDomainHost);
            NormalizeResponseTimestamps(item);
        }

        var totalPages = result.TotalItems == 0
            ? 0
            : (int)Math.Ceiling(result.TotalItems / (double)query.PageSize);

        return new ShortUrlListResponse
        {
            Items = result.Items,
            Pagination = new PaginationMetadata
            {
                Page = query.Page,
                PageSize = query.PageSize,
                TotalItems = result.TotalItems,
                TotalPages = totalPages,
                HasPreviousPage = query.Page > 1,
                HasNextPage = query.Page < totalPages
            },
            Filters = new ShortUrlFilterMetadata
            {
                Search = query.Search?.Trim(),
                IsActive = query.IsActive,
                Expiration = NormalizeExpiration(query.Expiration),
                IncludeDeleted = query.IncludeDeleted,
                CreatedFromUtc = query.CreatedFromUtc?.UtcDateTime,
                CreatedToUtc = query.CreatedToUtc?.UtcDateTime,
                SortBy = NormalizeSortField(query.SortBy),
                SortDirection = query.SortDirection.ToLowerInvariant()
            }
        };
    }

    public async Task<ShortUrlResponse> CreateAsync(
        CreateShortUrlRequest req,
        string clientIp,
        string? idempotencyKey,
        CancellationToken ct)
    {
        _ = clientIp;
        req.OriginalUrl = NormalizeDestinationUrl(req.OriginalUrl);
        var ownerId = RequireCurrentUserId();
        var nowUtc = _dateTimeProvider.UtcNow;
        var customDomain = await ResolveCustomDomainAsync(req.CustomDomainId, ownerId, ct);
        var idempotency = CreateIdempotencyContext(ownerId, req, idempotencyKey, nowUtc);
        ShortUrl entity;

        if (!string.IsNullOrWhiteSpace(req.CustomAlias))
        {
            entity = CreateEntity(req, req.CustomAlias, ownerId, customDomain, nowUtc);
            var creationResult = await TryCreateAsync(entity, idempotency, ct);
            if (creationResult.Outcome == IdempotentShortUrlCreationOutcome.ShortCodeConflict)
            {
                throw new AliasConflictException("Custom alias already exists.");
            }

            entity = ResolveCreatedEntity(creationResult, entity);
        }
        else
        {
            entity = null!;

            for (var attempt = 0; attempt < MaxGeneratedShortCodeAttempts; attempt++)
            {
                var generatedCode = _shortCodeGenerator.Generate(GeneratedShortCodeLength);
                if (!ShortUrlInputPolicy.IsValidShortCode(generatedCode) ||
                    ShortUrlInputPolicy.IsReservedAlias(generatedCode))
                {
                    continue;
                }

                var candidate = CreateEntity(req, generatedCode, ownerId, customDomain, nowUtc);
                var creationResult = await TryCreateAsync(candidate, idempotency, ct);
                if (creationResult.Outcome == IdempotentShortUrlCreationOutcome.RequestConflict)
                {
                    throw new IdempotencyKeyReusedException();
                }

                if (creationResult.Outcome == IdempotentShortUrlCreationOutcome.CustomDomainUnavailable)
                {
                    throw new CustomDomainUnavailableException();
                }

                if (creationResult.Outcome is
                    IdempotentShortUrlCreationOutcome.Created or
                    IdempotentShortUrlCreationOutcome.Existing)
                {
                    entity = ResolveCreatedEntity(creationResult, candidate);
                    break;
                }
            }

            if (entity == null)
            {
                throw new ShortCodeGenerationFailedException("Failed to generate unique short code.");
            }
        }

        await CacheRedirectAsync(entity, nowUtc, ct);

        var response = ToShortUrlResponse(entity);

        return response;
    }

    private async Task<IdempotentShortUrlCreationResult> TryCreateAsync(
        ShortUrl entity,
        ShortUrlIdempotencyContext? idempotency,
        CancellationToken ct)
    {
        if (idempotency != null)
        {
            return await _repository.TryCreateIdempotentAsync(entity, idempotency, ct);
        }

        var outcome = await _repository.TryCreateAsync(entity, ct);
        return outcome switch
        {
            ShortUrlCreationResult.Created =>
                new IdempotentShortUrlCreationResult(IdempotentShortUrlCreationOutcome.Created, entity),
            ShortUrlCreationResult.CustomDomainUnavailable =>
                new IdempotentShortUrlCreationResult(IdempotentShortUrlCreationOutcome.CustomDomainUnavailable),
            _ => new IdempotentShortUrlCreationResult(IdempotentShortUrlCreationOutcome.ShortCodeConflict)
        };
    }

    private static ShortUrl ResolveCreatedEntity(
        IdempotentShortUrlCreationResult result,
        ShortUrl candidate)
    {
        if (result.Outcome == IdempotentShortUrlCreationOutcome.RequestConflict)
        {
            throw new IdempotencyKeyReusedException();
        }

        if (result.Outcome == IdempotentShortUrlCreationOutcome.CustomDomainUnavailable)
        {
            throw new CustomDomainUnavailableException();
        }

        return result.Outcome switch
        {
            IdempotentShortUrlCreationOutcome.Created => candidate,
            IdempotentShortUrlCreationOutcome.Existing when result.ShortUrl != null => result.ShortUrl,
            _ => throw new InvalidOperationException("The idempotent creation result is inconsistent.")
        };
    }

    private ShortUrlIdempotencyContext? CreateIdempotencyContext(
        Guid ownerId,
        CreateShortUrlRequest request,
        string? idempotencyKey,
        DateTime nowUtc)
    {
        if (idempotencyKey == null)
        {
            return null;
        }

        return new ShortUrlIdempotencyContext(
            ownerId,
            HashText(idempotencyKey),
            HashCreateRequest(request),
            nowUtc,
            nowUtc.AddHours(_idempotencySettings.RetentionHours));
    }

    private static string HashCreateRequest(CreateShortUrlRequest request)
    {
        var alias = string.IsNullOrWhiteSpace(request.CustomAlias) ? string.Empty : request.CustomAlias;
        var expiresAtTicks = request.ExpiresAtUtc?.Ticks.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        var fields = new List<string>
        {
            request.CustomDomainId.HasValue ? "v2" : "v1",
            Convert.ToBase64String(Encoding.UTF8.GetBytes(request.OriginalUrl)),
            Convert.ToBase64String(Encoding.UTF8.GetBytes(alias)),
            expiresAtTicks
        };
        if (request.CustomDomainId.HasValue)
        {
            fields.Add(request.CustomDomainId.Value.ToString("D"));
        }

        var canonicalPayload = string.Join('\n', fields);

        return HashText(canonicalPayload);
    }

    private static string HashText(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static ShortUrl CreateEntity(
        CreateShortUrlRequest request,
        string shortCode,
        Guid ownerId,
        CustomDomain? customDomain,
        DateTime nowUtc)
    {
        var entity = new ShortUrl(ownerId)
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
        entity.RouteThrough(customDomain);
        return entity;
    }

    public async Task<ShortUrlResponse?> GetAsync(string shortCode, CancellationToken ct)
    {
        if (!ShortUrlInputPolicy.IsValidShortCode(shortCode))
        {
            return null;
        }

        var ownerId = RequireCurrentUserId();
        var entity = await _repository.GetOwnedByShortCodeNotDeletedAsync(shortCode, ownerId, ct);
        if (entity == null)
        {
            return null;
        }

        return ToShortUrlResponse(entity);
    }

    public async Task<ShortUrlResponse?> UpdateAsync(
        string shortCode,
        UpdateShortUrlRequest request,
        CancellationToken ct)
    {
        if (!ShortUrlInputPolicy.IsValidShortCode(shortCode))
        {
            return null;
        }

        request.OriginalUrl = NormalizeDestinationUrl(request.OriginalUrl);
        var ownerId = RequireCurrentUserId();
        var entity = await _repository.GetOwnedByShortCodeNotDeletedAsync(shortCode, ownerId, ct);
        if (entity == null)
        {
            return null;
        }

        var oldRoutingHost = GetRoutingHost(entity);
        var customDomain = await ResolveCustomDomainAsync(request.CustomDomainId, ownerId, ct);
        entity.OriginalUrl = request.OriginalUrl;
        entity.ExpiresAtUtc = request.ExpiresAtUtc;
        entity.RouteThrough(customDomain);
        var newRoutingHost = GetRoutingHost(entity);

        await _repository.SaveChangesAsync(ct);
        await _shortUrlCache.RemoveAsync(oldRoutingHost, shortCode, ct);
        if (!newRoutingHost.Equals(oldRoutingHost, StringComparison.Ordinal))
        {
            await _shortUrlCache.RemoveAsync(newRoutingHost, shortCode, ct);
        }

        return ToShortUrlResponse(entity);
    }

    public async Task<ShortUrlResponse?> SetStatusAsync(string shortCode, bool isActive, CancellationToken ct)
    {
        if (!ShortUrlInputPolicy.IsValidShortCode(shortCode))
        {
            return null;
        }

        var ownerId = RequireCurrentUserId();
        var entity = await _repository.GetOwnedByShortCodeNotDeletedAsync(shortCode, ownerId, ct);
        if (entity == null)
        {
            return null;
        }

        entity.IsActive = isActive;

        await _repository.SaveChangesAsync(ct);
        await RemoveCacheAsync(entity, ct);

        return ToShortUrlResponse(entity);
    }

    public async Task<bool> DeleteAsync(string shortCode, CancellationToken ct)
    {
        if (!ShortUrlInputPolicy.IsValidShortCode(shortCode))
        {
            return false;
        }

        var ownerId = RequireCurrentUserId();
        var entity = await _repository.GetOwnedByShortCodeNotDeletedAsync(shortCode, ownerId, ct);
        if (entity == null)
        {
            return false;
        }

        entity.IsDeleted = true;
        entity.DeletedAtUtc = _dateTimeProvider.UtcNow;

        await _repository.SaveChangesAsync(ct);
        await RemoveCacheAsync(entity, ct);

        return true;
    }

    public async Task<ShortUrlResponse> RestoreAsync(string shortCode, CancellationToken ct)
    {
        if (!ShortUrlInputPolicy.IsValidShortCode(shortCode))
        {
            throw new NotFoundException("Short URL not found.");
        }

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
        await RemoveCacheAsync(entity, ct);

        return ToShortUrlResponse(entity);
    }

    public async Task<StatsResponse?> GetStatsAsync(string shortCode, DateTime? fromUtc, DateTime? toUtc, CancellationToken ct)
    {
        if (!ShortUrlInputPolicy.IsValidShortCode(shortCode))
        {
            return null;
        }

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

    private Guid RequireCurrentUserId()
    {
        var userId = _currentUserContext.UserId;
        if (!userId.HasValue || userId.Value == Guid.Empty)
        {
            throw new AuthenticatedUserRequiredException();
        }

        return userId.Value;
    }

    private static string NormalizeDestinationUrl(string originalUrl)
    {
        if (ShortUrlInputPolicy.TryNormalizeDestinationUrl(originalUrl, out var normalizedUrl))
        {
            return normalizedUrl;
        }

        throw new FluentValidation.ValidationException(
            new[]
            {
                new FluentValidation.Results.ValidationFailure(
                    nameof(CreateShortUrlRequest.OriginalUrl),
                    "OriginalUrl must be an absolute http or https URL without credentials or surrounding whitespace.")
            });
    }

    private static ShortUrlExpirationFilter ParseExpiration(string value) =>
        value.ToLowerInvariant() switch
        {
            "expired" => ShortUrlExpirationFilter.Expired,
            "notexpired" => ShortUrlExpirationFilter.NotExpired,
            _ => ShortUrlExpirationFilter.All
        };

    private static string NormalizeExpiration(string value) =>
        value.ToLowerInvariant() switch
        {
            "expired" => "expired",
            "notexpired" => "notExpired",
            _ => "all"
        };

    private static ShortUrlSortField ParseSortField(string value) =>
        value.ToLowerInvariant() switch
        {
            "shortcode" => ShortUrlSortField.ShortCode,
            "clickcount" => ShortUrlSortField.ClickCount,
            "expiresat" => ShortUrlSortField.ExpiresAt,
            _ => ShortUrlSortField.CreatedAt
        };

    private static string NormalizeSortField(string value) =>
        value.ToLowerInvariant() switch
        {
            "shortcode" => "shortCode",
            "clickcount" => "clickCount",
            "expiresat" => "expiresAt",
            _ => "createdAt"
        };

    private async Task CacheRedirectAsync(ShortUrl entity, DateTime nowUtc, CancellationToken ct)
    {
        if (entity.CustomDomainId.HasValue &&
            entity.CustomDomain is not { CanServeBrandedLinks: true })
        {
            return;
        }

        var routingHost = GetRoutingHost(entity);
        var model = RedirectCachePolicy.CreateModel(entity, routingHost);
        var absoluteExpirationUtc = CalculateCacheExpiration(model.ExpiresAtUtc, nowUtc);
        if (absoluteExpirationUtc <= nowUtc)
        {
            return;
        }

        await _shortUrlCache.SetAsync(
            routingHost,
            entity.ShortCode,
            model,
            absoluteExpirationUtc,
            ct);
    }

    private static DateTime CalculateCacheExpiration(DateTime? linkExpiresAtUtc, DateTime nowUtc)
        => RedirectCachePolicy.CalculateAbsoluteExpiration(linkExpiresAtUtc, nowUtc);

    private ShortUrlResponse ToShortUrlResponse(ShortUrl entity)
    {
        var nowUtc = _dateTimeProvider.UtcNow;
        return new ShortUrlResponse
        {
            Id = entity.Id,
            OriginalUrl = entity.OriginalUrl,
            ShortCode = entity.ShortCode,
            ShortUrl = BuildPublicShortUrl(entity.ShortCode, entity.CustomDomain?.NormalizedHost),
            CustomDomainId = entity.CustomDomainId,
            CustomDomainHost = entity.CustomDomain?.NormalizedHost,
            CreatedAtUtc = AsUtc(entity.CreatedAtUtc),
            ExpiresAtUtc = AsUtc(entity.ExpiresAtUtc),
            IsActive = entity.IsActive,
            IsExpired = entity.ExpiresAtUtc.HasValue && entity.ExpiresAtUtc.Value <= nowUtc,
            IsDeleted = entity.IsDeleted,
            DeletedAtUtc = AsUtc(entity.DeletedAtUtc),
            RestoreUntilUtc = AsUtc(entity.DeletedAtUtc?.AddDays(_lifecycleSettings.RestoreRetentionDays)),
            ClickCount = entity.ClickCount,
            LastAccessedAtUtc = AsUtc(entity.LastAccessedAtUtc)
        };
    }

    private string BuildPublicShortUrl(string shortCode, string? customDomainHost)
    {
        var origin = customDomainHost == null
            ? _contractSettings.PublicBaseUrl
            : $"{_contractSettings.CustomDomainScheme}://{customDomainHost}";
        return $"{origin}/r/{Uri.EscapeDataString(shortCode)}";
    }

    private string GetRoutingHost(ShortUrl entity) =>
        entity.CustomDomain?.NormalizedHost ?? _contractSettings.DefaultHost;

    private Task RemoveCacheAsync(ShortUrl entity, CancellationToken ct) =>
        _shortUrlCache.RemoveAsync(GetRoutingHost(entity), entity.ShortCode, ct);

    private async Task<CustomDomain?> ResolveCustomDomainAsync(
        Guid? customDomainId,
        Guid ownerId,
        CancellationToken ct)
    {
        if (!customDomainId.HasValue)
        {
            return null;
        }

        if (customDomainId.Value == Guid.Empty)
        {
            throw new CustomDomainUnavailableException();
        }

        var customDomain = await _customDomainRepository.GetOwnedAsync(customDomainId.Value, ownerId, ct);
        if (customDomain is not { CanServeBrandedLinks: true })
        {
            throw new CustomDomainUnavailableException();
        }

        return customDomain;
    }

    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static DateTime? AsUtc(DateTime? value) =>
        value.HasValue ? AsUtc(value.Value) : null;

    private static void NormalizeResponseTimestamps(ShortUrlListItemResponse item)
    {
        item.CreatedAtUtc = AsUtc(item.CreatedAtUtc);
        item.ExpiresAtUtc = AsUtc(item.ExpiresAtUtc);
        item.DeletedAtUtc = AsUtc(item.DeletedAtUtc);
        item.RestoreUntilUtc = AsUtc(item.RestoreUntilUtc);
    }
}
