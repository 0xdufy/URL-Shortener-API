using System.Globalization;

namespace UrlShortener.Application.Security;

public sealed record DestinationHostPolicySettings(IReadOnlySet<string> BlockedHosts);

public sealed class DestinationHostPolicy
{
    private readonly IReadOnlySet<string> _blockedHosts;

    public DestinationHostPolicy(DestinationHostPolicySettings settings)
    {
        _blockedHosts = settings.BlockedHosts;
    }

    public bool IsBlocked(Uri destination)
    {
        if (!TryNormalizeHost(destination.IdnHost, out var host))
        {
            return true;
        }

        foreach (var blockedHost in _blockedHosts)
        {
            if (host.Equals(blockedHost, StringComparison.Ordinal) ||
                host.EndsWith('.' + blockedHost, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static bool TryNormalizeHost(string? value, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var candidate = value.Trim().TrimEnd('.');
        if (candidate.Length == 0)
        {
            return false;
        }

        try
        {
            normalized = new IdnMapping().GetAscii(candidate).ToLowerInvariant();
            var hostType = Uri.CheckHostName(normalized);
            return normalized.Length <= ShortUrlInputPolicy.MaximumReferrerHostLength &&
                hostType is UriHostNameType.Dns or UriHostNameType.IPv4 or UriHostNameType.IPv6;
        }
        catch (ArgumentException)
        {
            normalized = string.Empty;
            return false;
        }
    }
}
