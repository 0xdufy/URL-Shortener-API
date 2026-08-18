namespace UrlShortener.Application.Dtos;

public sealed class AnalyticsSummaryQuery
{
    public DateTimeOffset? FromUtc { get; set; }
    public DateTimeOffset? ToUtc { get; set; }
    public int TopReferrers { get; set; } = 10;
}
