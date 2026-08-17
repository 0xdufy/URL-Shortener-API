namespace UrlShortener.Api.Configuration;

public sealed class ProxyTrustOptions
{
    public const string SectionName = "ProxyTrust";

    public bool Enabled { get; set; }
    public int ForwardLimit { get; set; } = 1;
    public string[] KnownProxies { get; set; } = [];
    public string[] KnownNetworks { get; set; } = [];
}
