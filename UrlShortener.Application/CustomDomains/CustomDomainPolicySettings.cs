namespace UrlShortener.Application.CustomDomains;

public sealed record CustomDomainPolicySettings(
    string VerificationRecordLabel,
    string VerificationValuePrefix,
    IReadOnlySet<string> ReservedHosts)
{
    public bool IsReserved(string normalizedHost) => ReservedHosts.Any(reservedHost =>
        normalizedHost.Equals(reservedHost, StringComparison.Ordinal) ||
        normalizedHost.EndsWith('.' + reservedHost, StringComparison.Ordinal) ||
        reservedHost.EndsWith('.' + normalizedHost, StringComparison.Ordinal));
}
