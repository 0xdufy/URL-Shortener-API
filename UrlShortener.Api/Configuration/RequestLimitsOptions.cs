namespace UrlShortener.Api.Configuration;

public sealed class RequestLimitsOptions
{
    public const string SectionName = "RequestLimits";

    public long MaxRequestBodyBytes { get; set; } = 16_384;
    public int MaxRequestLineBytes { get; set; } = 8_192;
    public int MaxRequestHeadersTotalBytes { get; set; } = 16_384;
    public int MaxRequestHeaderCount { get; set; } = 64;
    public int RequestHeadersTimeoutSeconds { get; set; } = 10;
    public int RequestTimeoutSeconds { get; set; } = 15;
}
