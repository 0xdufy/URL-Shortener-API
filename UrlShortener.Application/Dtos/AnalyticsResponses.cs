namespace UrlShortener.Application.Dtos;

public sealed class AnalyticsSummaryResponse
{
    public string ShortCode { get; set; } = string.Empty;
    public AnalyticsRangeResponse Range { get; set; } = new();
    public long TotalClicks { get; set; }
    public long UniqueVisitorsEstimate { get; set; }
    public string UniqueVisitorMethod { get; set; } = "sumOfDailyPseudonymousVisitors";
    public AnalyticsBreakdownResponse Referrers { get; set; } = new();
    public AnalyticsBreakdownResponse Devices { get; set; } = new();
    public AnalyticsBreakdownResponse Browsers { get; set; } = new();
    public AnalyticsBreakdownResponse OperatingSystems { get; set; } = new();
    public AnalyticsFreshnessResponse Freshness { get; set; } = new();
}

public sealed class AnalyticsTimeSeriesResponse
{
    public string ShortCode { get; set; } = string.Empty;
    public AnalyticsRangeResponse Range { get; set; } = new();
    public string Granularity { get; set; } = string.Empty;
    public long TotalClicks { get; set; }
    public List<AnalyticsTimeBucketResponse> Buckets { get; set; } = new();
    public AnalyticsFreshnessResponse Freshness { get; set; } = new();
}

public sealed class AnalyticsRangeResponse
{
    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }
    public string BoundarySemantics { get; set; } = "[fromUtc,toUtc)";
    public string TimeZone { get; set; } = "UTC";
}

public sealed class AnalyticsBreakdownResponse
{
    public List<AnalyticsCategoryResponse> Items { get; set; } = new();
    public long OtherClicks { get; set; }
    public bool IsTruncated { get; set; }
}

public sealed class AnalyticsCategoryResponse
{
    public string Value { get; set; } = string.Empty;
    public long Clicks { get; set; }
}

public sealed class AnalyticsTimeBucketResponse
{
    public DateTime BucketStartUtc { get; set; }
    public DateTime BucketEndUtc { get; set; }
    public long Clicks { get; set; }
}

public sealed class AnalyticsFreshnessResponse
{
    public string Consistency { get; set; } = "eventual";
    public DateTime GeneratedAtUtc { get; set; }
    public DateTime? LastAggregatedAtUtc { get; set; }
    public bool IncludesOpenBucket { get; set; }
    public bool IsPartial { get; set; }
}
