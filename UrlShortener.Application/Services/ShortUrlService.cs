using AutoMapper;
using UrlShortener.Application.Dtos;
using UrlShortener.Application.Exceptions;
using UrlShortener.Application.Interfaces;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Application.Services;

public class ShortUrlService : IShortUrlService
{
    private readonly IShortUrlRepository _repository;
    private readonly IShortCodeGenerator _shortCodeGenerator;
    private readonly IShortUrlCache _shortUrlCache;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IMapper _mapper;

    public ShortUrlService(
        IShortUrlRepository repository,
        IShortCodeGenerator shortCodeGenerator,
        IShortUrlCache shortUrlCache,
        IDateTimeProvider dateTimeProvider,
        IMapper mapper)
    {
        _repository = repository;
        _shortCodeGenerator = shortCodeGenerator;
        _shortUrlCache = shortUrlCache;
        _dateTimeProvider = dateTimeProvider;
        _mapper = mapper;
    }

    public async Task<ShortUrlResponse> CreateAsync(CreateShortUrlRequest req, string baseHost, string clientIp, CancellationToken ct)
    {
        var nowUtc = _dateTimeProvider.UtcNow;
        string? shortCode = null;

        if (!string.IsNullOrWhiteSpace(req.CustomAlias))
        {
            var aliasExists = await _repository.ShortCodeExistsAsync(req.CustomAlias, ct);
            if (aliasExists)
            {
                throw new AliasConflictException("Custom alias already exists.");
            }

            shortCode = req.CustomAlias;
        }
        else
        {
            for (var i = 0; i < 5; i++)
            {
                var generated = _shortCodeGenerator.Generate(6);
                var exists = await _repository.ShortCodeExistsAsync(generated, ct);
                if (!exists)
                {
                    shortCode = generated;
                    break;
                }
            }

            if (shortCode == null)
            {
                throw new ShortCodeGenerationFailedException("Failed to generate unique short code.");
            }
        }

        var entity = new ShortUrl
        {
            Id = Guid.NewGuid(),
            OriginalUrl = req.OriginalUrl,
            ShortCode = shortCode,
            CreatedAtUtc = nowUtc,
            ExpiresAtUtc = req.ExpiresAtUtc,
            IsActive = true,
            IsDeleted = false,
            ClickCount = 0
        };

        await _repository.AddShortUrlAsync(entity, ct);
        await _repository.SaveChangesAsync(ct);

        var cacheModel = new ShortUrlCacheModel
        {
            OriginalUrl = entity.OriginalUrl,
            ExpiresAtUtc = entity.ExpiresAtUtc,
            IsActive = entity.IsActive,
            IsDeleted = entity.IsDeleted
        };

        await _shortUrlCache.SetAsync(entity.ShortCode, cacheModel, CalculateTtl(entity.ExpiresAtUtc, nowUtc), ct);

        var response = _mapper.Map<ShortUrlResponse>(entity);
        response.ShortUrl = $"{baseHost}/r/{entity.ShortCode}";

        return response;
    }

    public async Task<ShortUrlDetailsResponse?> GetAsync(string shortCode, CancellationToken ct)
    {
        var entity = await _repository.GetByShortCodeNotDeletedAsync(shortCode, ct);
        if (entity == null)
        {
            return null;
        }

        return _mapper.Map<ShortUrlDetailsResponse>(entity);
    }

    public async Task<ShortUrlDetailsResponse?> SetStatusAsync(string shortCode, bool isActive, CancellationToken ct)
    {
        var entity = await _repository.GetByShortCodeNotDeletedAsync(shortCode, ct);
        if (entity == null)
        {
            return null;
        }

        entity.IsActive = isActive;

        await _repository.SaveChangesAsync(ct);
        await _shortUrlCache.RemoveAsync(shortCode, ct);

        return _mapper.Map<ShortUrlDetailsResponse>(entity);
    }

    public async Task<bool> DeleteAsync(string shortCode, CancellationToken ct)
    {
        var entity = await _repository.GetByShortCodeNotDeletedAsync(shortCode, ct);
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

    public async Task<StatsResponse?> GetStatsAsync(string shortCode, DateTime? fromUtc, DateTime? toUtc, CancellationToken ct)
    {
        var entity = await _repository.GetByShortCodeNotDeletedAsync(shortCode, ct);
        if (entity == null)
        {
            return null;
        }

        var nowUtc = _dateTimeProvider.UtcNow;
        var actualToUtc = toUtc ?? nowUtc;
        var actualFromUtc = fromUtc ?? nowUtc.AddDays(-30);

        var logs = await _repository.GetAccessLogsAsync(entity.Id, actualFromUtc, actualToUtc, ct);

        var dailyClicks = logs
            .GroupBy(x => x.AccessedAtUtc.Date)
            .OrderBy(x => x.Key)
            .Select(x => new DailyClicksItem
            {
                DateUtc = x.Key.ToString("yyyy-MM-dd"),
                Clicks = x.Count()
            })
            .ToList();

        return new StatsResponse
        {
            ShortCode = entity.ShortCode,
            TotalClicks = logs.LongCount(),
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

            var fromDb = await _repository.GetByShortCodeAnyAsync(shortCode, ct);
            if (fromDb == null || fromDb.IsDeleted || !fromDb.IsActive)
            {
                return (404, null);
            }

            if (fromDb.ExpiresAtUtc.HasValue && fromDb.ExpiresAtUtc.Value <= nowUtc)
            {
                return (410, null);
            }

            await RegisterAccessAsync(fromDb, nowUtc, ip, userAgent, referer, ct);
            return (302, cacheModel.OriginalUrl);
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
            OriginalUrl = entity.OriginalUrl,
            ExpiresAtUtc = entity.ExpiresAtUtc,
            IsActive = entity.IsActive,
            IsDeleted = entity.IsDeleted
        };

        await _shortUrlCache.SetAsync(entity.ShortCode, model, CalculateTtl(entity.ExpiresAtUtc, nowUtc), ct);

        await RegisterAccessAsync(entity, nowUtc, ip, userAgent, referer, ct);

        return (302, entity.OriginalUrl);
    }

    private async Task RegisterAccessAsync(ShortUrl entity, DateTime nowUtc, string ip, string? userAgent, string? referer, CancellationToken ct)
    {
        entity.ClickCount += 1;
        entity.LastAccessedAtUtc = nowUtc;

        var log = new ShortUrlAccessLog
        {
            Id = Guid.NewGuid(),
            ShortUrlId = entity.Id,
            AccessedAtUtc = nowUtc,
            IpAddress = ip,
            UserAgent = userAgent,
            Referer = referer
        };

        await _repository.AddAccessLogAsync(log, ct);
        await _repository.SaveChangesAsync(ct);
    }

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
}
