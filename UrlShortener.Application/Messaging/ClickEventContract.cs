namespace UrlShortener.Application.Messaging;

public static class ClickEventContract
{
    public const string EventName = "analytics.click";
    public const int Version = 1;
    public const string VisitorIdentityScheme = "hmac-sha256-utc-day-v1";
}

public sealed record ClickEventV1(
    Guid ShortUrlId,
    DateTimeOffset AccessedAtUtc,
    string? ReferrerHost,
    string? UserAgent,
    string PseudonymousVisitorId,
    DateOnly VisitorIdentityPeriodUtc,
    string VisitorIdentityScheme);
