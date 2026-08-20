namespace UrlShortener.Infrastructure.Configuration;

public sealed class CustomDomainOptions
{
    public const string SectionName = "CustomDomains";

    public string VerificationRecordLabel { get; set; } = "_urlshortener-verification";
    public string VerificationValuePrefix { get; set; } = "urlshortener-verification=";
    public string DnsOverHttpsEndpoint { get; set; } = "https://cloudflare-dns.com/dns-query";
    public int LookupTimeoutSeconds { get; set; } = 5;
    public string[] ReservedHosts { get; set; } = [];
}
