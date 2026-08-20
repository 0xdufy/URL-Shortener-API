using System.Globalization;
using System.Net;

namespace UrlShortener.Application.CustomDomains;

public static class CustomDomainHostNormalizer
{
    private static readonly IdnMapping IdnMapping = new();

    public static bool TryNormalize(string? value, out string normalizedHost, out string error)
    {
        normalizedHost = string.Empty;
        error = "Host must be a DNS hostname without a scheme, path, query, fragment, wildcard, or port.";

        if (string.IsNullOrWhiteSpace(value) || value != value.Trim() ||
            value.Contains("://", StringComparison.Ordinal) ||
            value.IndexOfAny(['/', '\\', '?', '#', ':', '*', '@']) >= 0)
        {
            return false;
        }

        var candidate = value.EndsWith(".", StringComparison.Ordinal) ? value[..^1] : value;
        if (candidate.Length == 0 || candidate.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            candidate = IdnMapping.GetAscii(candidate).ToLowerInvariant();
        }
        catch (ArgumentException)
        {
            return false;
        }

        var labels = candidate.Split('.');
        if (candidate.Length > 253 || labels.Length < 2 ||
            labels.Any(label => label.Length is < 1 or > 63 ||
                label[0] == '-' || label[^1] == '-' ||
                label.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-')) ||
            IPAddress.TryParse(candidate, out _))
        {
            return false;
        }

        normalizedHost = candidate;
        error = string.Empty;
        return true;
    }
}
