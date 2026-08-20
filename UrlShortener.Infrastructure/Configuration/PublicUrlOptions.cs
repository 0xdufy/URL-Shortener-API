namespace UrlShortener.Infrastructure.Configuration;

public sealed class PublicUrlOptions
{
    public const string SectionName = "PublicUrls";

    public string BaseUrl { get; set; } = string.Empty;
    public string CustomDomainScheme { get; set; } = "https";
}
