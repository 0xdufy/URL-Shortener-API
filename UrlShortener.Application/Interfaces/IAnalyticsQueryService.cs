using UrlShortener.Application.Dtos;

namespace UrlShortener.Application.Interfaces;

public interface IAnalyticsQueryService
{
    Task<AnalyticsSummaryResponse?> GetSummaryAsync(AnalyticsSummaryQuery query, string shortCode, CancellationToken ct);
    Task<AnalyticsTimeSeriesResponse?> GetTimeSeriesAsync(AnalyticsTimeSeriesQuery query, string shortCode, CancellationToken ct);
}
