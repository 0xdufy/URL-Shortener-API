using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using UrlShortener.Application.Dtos;
using UrlShortener.Application.Interfaces;

namespace UrlShortener.Infrastructure.Caching;

public sealed class ShortUrlCache : IShortUrlCache
{
    private const string CacheKeyPrefix = "redirect:v1:";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<ShortUrlCache> _logger;

    public ShortUrlCache(
        IDistributedCache distributedCache,
        ILogger<ShortUrlCache> logger)
    {
        _distributedCache = distributedCache;
        _logger = logger;
    }

    public async Task<ShortUrlCacheModel?> GetAsync(string shortCode, CancellationToken ct)
    {
        try
        {
            var payload = await _distributedCache.GetAsync(GetKey(shortCode), ct);
            if (payload == null)
            {
                return null;
            }

            var model = JsonSerializer.Deserialize<ShortUrlCacheModel>(payload, SerializerOptions);
            if (model != null && IsValid(model))
            {
                return model;
            }

            _logger.LogWarning(
                "Ignoring redirect cache entry for short code {ShortCode} because its schema or data is invalid.",
                shortCode);
            await RemoveAsync(shortCode, ct);
            return null;
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(
                exception,
                "Ignoring malformed redirect cache entry for short code {ShortCode}.",
                shortCode);
            await RemoveAsync(shortCode, ct);
            return null;
        }
        catch (Exception exception) when (IsBackendFailure(exception))
        {
            _logger.LogWarning(
                exception,
                "Redirect cache read failed for short code {ShortCode}; persistence will be used.",
                shortCode);
            return null;
        }
    }

    public async Task SetAsync(
        string shortCode,
        ShortUrlCacheModel model,
        DateTime absoluteExpirationUtc,
        CancellationToken ct)
    {
        var normalizedExpirationUtc = AsUtc(absoluteExpirationUtc);
        if (normalizedExpirationUtc <= DateTime.UtcNow)
        {
            return;
        }

        try
        {
            var payload = JsonSerializer.SerializeToUtf8Bytes(model, SerializerOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = new DateTimeOffset(normalizedExpirationUtc)
            };

            await _distributedCache.SetAsync(GetKey(shortCode), payload, options, ct);
        }
        catch (Exception exception) when (IsBackendFailure(exception))
        {
            _logger.LogWarning(
                exception,
                "Redirect cache write failed for short code {ShortCode}; the persisted redirect remains authoritative.",
                shortCode);
        }
    }

    public async Task RemoveAsync(string shortCode, CancellationToken ct)
    {
        try
        {
            await _distributedCache.RemoveAsync(GetKey(shortCode), ct);
        }
        catch (Exception exception) when (IsBackendFailure(exception))
        {
            _logger.LogWarning(
                exception,
                "Redirect cache invalidation failed for short code {ShortCode}; stale entries remain guarded by persisted redirect state.",
                shortCode);
        }
    }

    private static string GetKey(string shortCode)
    {
        return $"{CacheKeyPrefix}{shortCode}";
    }

    private static bool IsValid(ShortUrlCacheModel model) =>
        model.SchemaVersion == ShortUrlCacheModel.CurrentSchemaVersion &&
        model.ShortUrlId != Guid.Empty &&
        !string.IsNullOrWhiteSpace(model.OriginalUrl);

    private static bool IsBackendFailure(Exception exception) =>
        exception is RedisException or TimeoutException;

    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
