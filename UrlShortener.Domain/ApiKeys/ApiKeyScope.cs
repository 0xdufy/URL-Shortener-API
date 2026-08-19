namespace UrlShortener.Domain.ApiKeys;

[Flags]
public enum ApiKeyScope
{
    None = 0,
    ShortUrlsCreate = 1 << 0,
    ShortUrlsRead = 1 << 1,
    ShortUrlsWrite = 1 << 2,
    AnalyticsRead = 1 << 3
}
