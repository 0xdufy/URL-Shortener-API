using UrlShortener.Domain.Analytics;

namespace UrlShortener.Application.Interfaces;

public sealed record AnalyticsDimensionCount(string Value, long Clicks);

public sealed record AnalyticsSummaryReadModel(
    string ShortCode,
    long TotalClicks,
    long UniqueVisitorsEstimate,
    DateTime? LastAggregatedAtUtc,
    IReadOnlyList<AnalyticsDimensionCount> Referrers,
    IReadOnlyList<AnalyticsDimensionCount> Devices,
    IReadOnlyList<AnalyticsDimensionCount> Browsers,
    IReadOnlyList<AnalyticsDimensionCount> OperatingSystems);

public sealed record AnalyticsBucketReadModel(
    DateTime BucketStartUtc,
    long Clicks,
    DateTime UpdatedAtUtc);

public sealed record AnalyticsTimeSeriesReadModel(
    string ShortCode,
    IReadOnlyList<AnalyticsBucketReadModel> Buckets);

public sealed record AnalyticsTimeSeriesCriteria(
    Guid OwnerId,
    string ShortCode,
    DateTime FromUtc,
    DateTime ToUtc,
    AnalyticsBucketGranularity Granularity);

public sealed record AnalyticsSummaryCriteria(
    Guid OwnerId,
    string ShortCode,
    DateTime FromUtc,
    DateTime ToUtc,
    int TopReferrers);
