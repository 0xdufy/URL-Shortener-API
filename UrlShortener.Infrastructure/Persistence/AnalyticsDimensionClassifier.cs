using System.Globalization;

namespace UrlShortener.Infrastructure.Persistence;

internal static class AnalyticsDimensionClassifier
{
    public const short SchemaVersion = 1;
    public const string Overall = "All";
    public const string Direct = "Direct";
    public const string Unknown = "Unknown";
    public const string Other = "Other";

    public static ClickAnalyticsDimensions Classify(string? referrerHost, string? userAgent)
    {
        return new ClickAnalyticsDimensions(
            ClassifyReferrer(referrerHost),
            ClassifyDevice(userAgent),
            ClassifyBrowser(userAgent),
            ClassifyOperatingSystem(userAgent));
    }

    private static string ClassifyReferrer(string? referrerHost)
    {
        if (string.IsNullOrWhiteSpace(referrerHost))
        {
            return Direct;
        }

        var candidate = referrerHost.Trim().TrimEnd('.');
        if (candidate.Length == 0 || candidate.Length > 253 || ContainsControlCharacter(candidate))
        {
            return Unknown;
        }

        try
        {
            var asciiHost = new IdnMapping().GetAscii(candidate).ToLowerInvariant();
            return Uri.CheckHostName(asciiHost) == UriHostNameType.Unknown
                ? Unknown
                : asciiHost;
        }
        catch (ArgumentException)
        {
            return Unknown;
        }
    }

    private static string ClassifyDevice(string? userAgent)
    {
        if (!TryUseUserAgent(userAgent, out var value))
        {
            return Unknown;
        }

        if (ContainsAny(value, "bot", "spider", "crawler", "slurp", "headless"))
        {
            return "Bot";
        }

        if (ContainsAny(value, "ipad", "tablet", "kindle", "silk/"))
        {
            return "Tablet";
        }

        if (ContainsAny(value, "iphone", "ipod", "mobile") ||
            value.Contains("android", StringComparison.OrdinalIgnoreCase))
        {
            return "Mobile";
        }

        if (ContainsAny(value, "windows", "macintosh", "x11", "cros", "linux"))
        {
            return "Desktop";
        }

        return Other;
    }

    private static string ClassifyBrowser(string? userAgent)
    {
        if (!TryUseUserAgent(userAgent, out var value))
        {
            return Unknown;
        }

        if (ContainsAny(value, "edg/", "edga/", "edgios/"))
        {
            return "Edge";
        }

        if (value.Contains("opr/", StringComparison.OrdinalIgnoreCase))
        {
            return "Opera";
        }

        if (ContainsAny(value, "chrome/", "crios/"))
        {
            return "Chrome";
        }

        if (ContainsAny(value, "firefox/", "fxios/"))
        {
            return "Firefox";
        }

        if (value.Contains("safari/", StringComparison.OrdinalIgnoreCase) &&
            value.Contains("version/", StringComparison.OrdinalIgnoreCase))
        {
            return "Safari";
        }

        if (ContainsAny(value, "msie ", "trident/"))
        {
            return "Internet Explorer";
        }

        return Other;
    }

    private static string ClassifyOperatingSystem(string? userAgent)
    {
        if (!TryUseUserAgent(userAgent, out var value))
        {
            return Unknown;
        }

        if (ContainsAny(value, "iphone", "ipad", "ipod"))
        {
            return "iOS";
        }

        if (value.Contains("android", StringComparison.OrdinalIgnoreCase))
        {
            return "Android";
        }

        if (value.Contains("windows", StringComparison.OrdinalIgnoreCase))
        {
            return "Windows";
        }

        if (ContainsAny(value, "macintosh", "mac os x"))
        {
            return "macOS";
        }

        if (ContainsAny(value, "linux", "x11"))
        {
            return "Linux";
        }

        return Other;
    }

    private static bool TryUseUserAgent(string? userAgent, out string value)
    {
        value = userAgent?.Trim() ?? string.Empty;
        return value.Length > 0 && !ContainsControlCharacter(value);
    }

    private static bool ContainsControlCharacter(string value)
    {
        return value.Any(char.IsControl);
    }

    private static bool ContainsAny(string value, params string[] candidates)
    {
        return candidates.Any(candidate => value.Contains(candidate, StringComparison.OrdinalIgnoreCase));
    }
}

internal sealed record ClickAnalyticsDimensions(
    string Referrer,
    string Device,
    string Browser,
    string OperatingSystem);
