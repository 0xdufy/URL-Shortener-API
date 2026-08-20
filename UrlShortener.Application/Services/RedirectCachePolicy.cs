using UrlShortener.Application.Dtos;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Application.Services;

internal static class RedirectCachePolicy
{
    private static readonly TimeSpan MaximumLifetime = TimeSpan.FromHours(24);

    public static ShortUrlCacheModel CreateModel(ShortUrl entity, string routingHost) =>
        new()
        {
            ShortUrlId = entity.Id,
            RoutingHost = routingHost,
            OriginalUrl = entity.OriginalUrl,
            ExpiresAtUtc = AsUtc(entity.ExpiresAtUtc)
        };

    public static ShortUrlCacheModel CreateModel(RedirectLookupModel redirect) =>
        new()
        {
            ShortUrlId = redirect.ShortUrlId,
            RoutingHost = redirect.RoutingHost,
            OriginalUrl = redirect.OriginalUrl,
            ExpiresAtUtc = AsUtc(redirect.ExpiresAtUtc)
        };

    public static DateTime CalculateAbsoluteExpiration(DateTime? linkExpiresAtUtc, DateTime nowUtc)
    {
        var maximumExpirationUtc = nowUtc.Add(MaximumLifetime);
        return linkExpiresAtUtc.HasValue && linkExpiresAtUtc.Value < maximumExpirationUtc
            ? linkExpiresAtUtc.Value
            : maximumExpirationUtc;
    }

    private static DateTime? AsUtc(DateTime? value)
    {
        if (!value.HasValue || value.Value.Kind == DateTimeKind.Utc)
        {
            return value;
        }

        return DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
    }
}
