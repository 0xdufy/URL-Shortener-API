namespace UrlShortener.Application.Security;

public static class ShortUrlInputPolicy
{
    public const int MinimumShortCodeLength = 4;
    public const int MaximumShortCodeLength = 20;
    public const int MaximumDestinationUrlLength = 2048;
    public const int MaximumUserAgentLength = 256;
    public const int MaximumRawReferrerLength = 2048;
    public const int MaximumReferrerHostLength = 253;

    private static readonly string[] ReservedRouteRoots =
    [
        "api",
        "auth",
        "health",
        "healthz",
        "live",
        "livez",
        "ready",
        "readyz",
        "metrics",
        "swagger",
        "openapi",
        "docs",
        "app",
        "r",
        "dashboard",
        "links",
        "analytics",
        "api-keys",
        "domains",
        "account",
        "sign-in",
        "register"
    ];

    public static bool IsValidShortCode(string? value)
    {
        if (value is null || value.Length is < MinimumShortCodeLength or > MaximumShortCodeLength)
        {
            return false;
        }

        return value.All(IsShortCodeCharacter);
    }

    public static bool IsReservedAlias(string value)
    {
        foreach (var root in ReservedRouteRoots)
        {
            if (value.Equals(root, StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith(string.Concat(root, "-"), StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith(string.Concat(root, "_"), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static bool TryNormalizeDestinationUrl(string? value, out string normalizedUrl)
    {
        normalizedUrl = string.Empty;
        if (string.IsNullOrEmpty(value) ||
            value.Length > MaximumDestinationUrlLength ||
            !value.Equals(value.Trim(), StringComparison.Ordinal) ||
            value.Any(character => char.IsControl(character)))
        {
            return false;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (!uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
             !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) ||
            string.IsNullOrEmpty(uri.Host) ||
            uri.HostNameType is UriHostNameType.Basic or UriHostNameType.Unknown ||
            !string.IsNullOrEmpty(uri.UserInfo))
        {
            return false;
        }

        try
        {
            var idnHost = uri.IdnHost.TrimEnd('.').ToLowerInvariant();
            if (string.IsNullOrEmpty(idnHost) ||
                (uri.HostNameType == UriHostNameType.Dns && idnHost.Length > MaximumReferrerHostLength))
            {
                return false;
            }

            var builder = new UriBuilder(uri)
            {
                Host = idnHost
            };
            normalizedUrl = builder.Uri.AbsoluteUri;
            return normalizedUrl.Length <= MaximumDestinationUrlLength;
        }
        catch (UriFormatException)
        {
            normalizedUrl = string.Empty;
            return false;
        }
    }

    private static bool IsShortCodeCharacter(char value) =>
        value is >= 'A' and <= 'Z' or
            >= 'a' and <= 'z' or
            >= '0' and <= '9' or
            '_' or '-';
}
