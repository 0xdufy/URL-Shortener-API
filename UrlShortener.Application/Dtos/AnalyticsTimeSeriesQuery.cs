namespace UrlShortener.Application.Dtos;

public sealed class AnalyticsTimeSeriesQuery
{
    public DateTimeOffset? FromUtc { get; set; }
    public DateTimeOffset? ToUtc { get; set; }
    public string Granularity { get; set; } = "day";
}
