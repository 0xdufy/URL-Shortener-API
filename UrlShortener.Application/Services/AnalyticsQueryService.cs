using FluentValidation;
using FluentValidation.Results;
using UrlShortener.Application.Dtos;
using UrlShortener.Application.Exceptions;
using UrlShortener.Application.Interfaces;
using UrlShortener.Application.Security;
using UrlShortener.Domain.Analytics;

namespace UrlShortener.Application.Services;

public sealed class AnalyticsQueryService : IAnalyticsQueryService
{
    private const int MaximumSummaryDays = 366;
    private const int MaximumHourlyBuckets = 31 * 24;
    private const int MaximumDailyBuckets = 731;

    private readonly IShortUrlRepository _repository;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AnalyticsQueryService(
        IShortUrlRepository repository,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _currentUserContext = currentUserContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<AnalyticsSummaryResponse?> GetSummaryAsync(
        AnalyticsSummaryQuery query,
        string shortCode,
        CancellationToken ct)
    {
        if (!ShortUrlInputPolicy.IsValidShortCode(shortCode))
        {
            return null;
        }

        var nowUtc = AsUtc(_dateTimeProvider.UtcNow);
        var maximumToUtc = StartOfDay(nowUtc).AddDays(1);
        var toUtc = query.ToUtc?.UtcDateTime ?? maximumToUtc;
        var fromUtc = query.FromUtc?.UtcDateTime ?? toUtc.AddDays(-30);

        EnsureAligned(fromUtc, TimeSpan.FromDays(1), nameof(query.FromUtc));
        EnsureAligned(toUtc, TimeSpan.FromDays(1), nameof(query.ToUtc));
        EnsureRange(fromUtc, toUtc, maximumToUtc, TimeSpan.FromDays(MaximumSummaryDays));

        var model = await _repository.GetAnalyticsSummaryAsync(
            new AnalyticsSummaryCriteria(
                RequireCurrentUserId(),
                shortCode,
                fromUtc,
                toUtc,
                query.TopReferrers),
            ct);

        if (model is null)
        {
            return null;
        }

        return new AnalyticsSummaryResponse
        {
            ShortCode = model.ShortCode,
            Range = CreateRange(fromUtc, toUtc),
            TotalClicks = model.TotalClicks,
            UniqueVisitorsEstimate = model.UniqueVisitorsEstimate,
            Referrers = CreateReferrerBreakdown(model.Referrers, model.TotalClicks),
            Devices = CreateCompleteBreakdown(model.Devices),
            Browsers = CreateCompleteBreakdown(model.Browsers),
            OperatingSystems = CreateCompleteBreakdown(model.OperatingSystems),
            Freshness = CreateFreshness(nowUtc, model.LastAggregatedAtUtc, fromUtc, toUtc, StartOfDay(nowUtc))
        };
    }

    public async Task<AnalyticsTimeSeriesResponse?> GetTimeSeriesAsync(
        AnalyticsTimeSeriesQuery query,
        string shortCode,
        CancellationToken ct)
    {
        if (!ShortUrlInputPolicy.IsValidShortCode(shortCode))
        {
            return null;
        }

        var granularity = ParseGranularity(query.Granularity);
        var bucketSize = granularity == AnalyticsBucketGranularity.Hour
            ? TimeSpan.FromHours(1)
            : TimeSpan.FromDays(1);
        var maximumBuckets = granularity == AnalyticsBucketGranularity.Hour
            ? MaximumHourlyBuckets
            : MaximumDailyBuckets;
        var nowUtc = AsUtc(_dateTimeProvider.UtcNow);
        var openBucketStartUtc = granularity == AnalyticsBucketGranularity.Hour
            ? StartOfHour(nowUtc)
            : StartOfDay(nowUtc);
        var maximumToUtc = openBucketStartUtc.Add(bucketSize);
        var defaultBucketCount = granularity == AnalyticsBucketGranularity.Hour ? 24 : 30;
        var toUtc = query.ToUtc?.UtcDateTime ?? maximumToUtc;
        var fromUtc = query.FromUtc?.UtcDateTime ?? toUtc.Subtract(TimeSpan.FromTicks(bucketSize.Ticks * defaultBucketCount));

        EnsureAligned(fromUtc, bucketSize, nameof(query.FromUtc));
        EnsureAligned(toUtc, bucketSize, nameof(query.ToUtc));
        EnsureRange(fromUtc, toUtc, maximumToUtc, TimeSpan.FromTicks(bucketSize.Ticks * maximumBuckets));

        var model = await _repository.GetAnalyticsTimeSeriesAsync(
            new AnalyticsTimeSeriesCriteria(
                RequireCurrentUserId(),
                shortCode,
                fromUtc,
                toUtc,
                granularity),
            ct);

        if (model is null)
        {
            return null;
        }

        var counts = model.Buckets.ToDictionary(x => x.BucketStartUtc, x => x.Clicks);
        var buckets = new List<AnalyticsTimeBucketResponse>();
        for (var bucketStartUtc = fromUtc; bucketStartUtc < toUtc; bucketStartUtc = bucketStartUtc.Add(bucketSize))
        {
            buckets.Add(new AnalyticsTimeBucketResponse
            {
                BucketStartUtc = bucketStartUtc,
                BucketEndUtc = bucketStartUtc.Add(bucketSize),
                Clicks = counts.GetValueOrDefault(bucketStartUtc)
            });
        }

        return new AnalyticsTimeSeriesResponse
        {
            ShortCode = model.ShortCode,
            Range = CreateRange(fromUtc, toUtc),
            Granularity = granularity == AnalyticsBucketGranularity.Hour ? "hour" : "day",
            TotalClicks = buckets.Sum(x => x.Clicks),
            Buckets = buckets,
            Freshness = CreateFreshness(
                nowUtc,
                model.Buckets.Count == 0 ? null : model.Buckets.Max(x => x.UpdatedAtUtc),
                fromUtc,
                toUtc,
                openBucketStartUtc)
        };
    }

    private Guid RequireCurrentUserId()
    {
        var userId = _currentUserContext.UserId;
        if (!userId.HasValue || userId.Value == Guid.Empty)
        {
            throw new AuthenticatedUserRequiredException();
        }

        return userId.Value;
    }

    private static AnalyticsBucketGranularity ParseGranularity(string value) =>
        string.Equals(value, "hour", StringComparison.OrdinalIgnoreCase)
            ? AnalyticsBucketGranularity.Hour
            : AnalyticsBucketGranularity.Day;

    private static AnalyticsRangeResponse CreateRange(DateTime fromUtc, DateTime toUtc) => new()
    {
        FromUtc = fromUtc,
        ToUtc = toUtc
    };

    private static AnalyticsBreakdownResponse CreateReferrerBreakdown(
        IReadOnlyList<AnalyticsDimensionCount> items,
        long totalClicks)
    {
        var returnedClicks = items.Sum(x => x.Clicks);
        return new AnalyticsBreakdownResponse
        {
            Items = ToCategories(items),
            OtherClicks = Math.Max(0, totalClicks - returnedClicks),
            IsTruncated = returnedClicks < totalClicks
        };
    }

    private static AnalyticsBreakdownResponse CreateCompleteBreakdown(
        IReadOnlyList<AnalyticsDimensionCount> items) => new()
    {
        Items = ToCategories(items)
    };

    private static List<AnalyticsCategoryResponse> ToCategories(IReadOnlyList<AnalyticsDimensionCount> items) =>
        items.Select(x => new AnalyticsCategoryResponse { Value = x.Value, Clicks = x.Clicks }).ToList();

    private static AnalyticsFreshnessResponse CreateFreshness(
        DateTime nowUtc,
        DateTime? lastAggregatedAtUtc,
        DateTime fromUtc,
        DateTime toUtc,
        DateTime openBucketStartUtc)
    {
        var includesOpenBucket = fromUtc <= openBucketStartUtc && toUtc > openBucketStartUtc;
        return new AnalyticsFreshnessResponse
        {
            GeneratedAtUtc = nowUtc,
            LastAggregatedAtUtc = lastAggregatedAtUtc.HasValue ? AsUtc(lastAggregatedAtUtc.Value) : null,
            IncludesOpenBucket = includesOpenBucket,
            IsPartial = includesOpenBucket
        };
    }

    private static void EnsureAligned(DateTime value, TimeSpan bucketSize, string field)
    {
        var aligned = bucketSize == TimeSpan.FromHours(1)
            ? value.Minute == 0 && value.Second == 0 && value.Millisecond == 0 && value.Ticks % TimeSpan.TicksPerSecond == 0
            : value.TimeOfDay == TimeSpan.Zero;

        if (!aligned)
        {
            var boundary = bucketSize == TimeSpan.FromHours(1) ? "whole UTC hour" : "UTC midnight";
            ThrowValidation(field, $"{field} must be aligned to a {boundary} boundary.");
        }
    }

    private static void EnsureRange(DateTime fromUtc, DateTime toUtc, DateTime maximumToUtc, TimeSpan maximumRange)
    {
        if (fromUtc >= toUtc)
        {
            ThrowValidation(nameof(AnalyticsSummaryQuery.ToUtc), "ToUtc must be greater than FromUtc.");
        }

        if (toUtc > maximumToUtc)
        {
            ThrowValidation(nameof(AnalyticsSummaryQuery.ToUtc), "ToUtc cannot extend beyond the end of the current open UTC bucket.");
        }

        if (toUtc - fromUtc > maximumRange)
        {
            ThrowValidation(nameof(AnalyticsSummaryQuery.FromUtc), $"The requested range cannot exceed {maximumRange.TotalDays:0.##} days.");
        }
    }

    private static void ThrowValidation(string field, string message) =>
        throw new ValidationException([new ValidationFailure(field, message)]);

    private static DateTime StartOfHour(DateTime value) =>
        new(value.Year, value.Month, value.Day, value.Hour, 0, 0, DateTimeKind.Utc);

    private static DateTime StartOfDay(DateTime value) =>
        DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);

    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
