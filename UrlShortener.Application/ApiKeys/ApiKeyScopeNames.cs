using UrlShortener.Domain.ApiKeys;

namespace UrlShortener.Application.ApiKeys;

public static class ApiKeyScopeNames
{
    public const string ShortUrlsCreate = "shorturls:create";
    public const string ShortUrlsRead = "shorturls:read";
    public const string ShortUrlsWrite = "shorturls:write";
    public const string AnalyticsRead = "analytics:read";

    public static bool TryParse(string value, out ApiKeyScope scope)
    {
        scope = value switch
        {
            ShortUrlsCreate => ApiKeyScope.ShortUrlsCreate,
            ShortUrlsRead => ApiKeyScope.ShortUrlsRead,
            ShortUrlsWrite => ApiKeyScope.ShortUrlsWrite,
            AnalyticsRead => ApiKeyScope.AnalyticsRead,
            _ => ApiKeyScope.None
        };

        return scope != ApiKeyScope.None;
    }

    public static IReadOnlyList<string> ToNames(ApiKeyScope scopes)
    {
        var names = new List<string>(4);
        AddIfPresent(names, scopes, ApiKeyScope.ShortUrlsCreate, ShortUrlsCreate);
        AddIfPresent(names, scopes, ApiKeyScope.ShortUrlsRead, ShortUrlsRead);
        AddIfPresent(names, scopes, ApiKeyScope.ShortUrlsWrite, ShortUrlsWrite);
        AddIfPresent(names, scopes, ApiKeyScope.AnalyticsRead, AnalyticsRead);
        return names;
    }

    private static void AddIfPresent(
        ICollection<string> names,
        ApiKeyScope scopes,
        ApiKeyScope candidate,
        string name)
    {
        if ((scopes & candidate) == candidate)
        {
            names.Add(name);
        }
    }
}
