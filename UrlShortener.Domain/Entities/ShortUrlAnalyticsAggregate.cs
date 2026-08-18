using UrlShortener.Domain.Analytics;

namespace UrlShortener.Domain.Entities;

public class ShortUrlAnalyticsAggregate
{
    public Guid ShortUrlId { get; set; }
    public ShortUrl ShortUrl { get; set; } = null!;
    public DateTime BucketStartUtc { get; set; }
    public AnalyticsBucketGranularity Granularity { get; set; }
    public AnalyticsDimension Dimension { get; set; }
    public short DimensionSchemaVersion { get; set; }
    public string DimensionValue { get; set; } = string.Empty;
    public long ClickCount { get; set; }
    public long UniqueVisitorCount { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
