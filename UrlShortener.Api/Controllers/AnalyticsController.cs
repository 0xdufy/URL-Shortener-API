using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Models;
using UrlShortener.Api.RateLimiting;
using UrlShortener.Api.Security;
using UrlShortener.Application.Dtos;
using UrlShortener.Application.Interfaces;
using UrlShortener.Application.RateLimiting;

namespace UrlShortener.Api.Controllers;

[ApiController]
[Authorize(Policy = ApiKeyAuthorizationPolicies.AnalyticsRead)]
[DistributedRateLimit(RateLimitPolicy.Authenticated)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
[Route("api/v1/short-urls/{shortCode}/analytics")]
public sealed class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsQueryService _analyticsQueryService;
    private readonly IValidator<AnalyticsSummaryQuery> _summaryValidator;
    private readonly IValidator<AnalyticsTimeSeriesQuery> _timeSeriesValidator;

    public AnalyticsController(
        IAnalyticsQueryService analyticsQueryService,
        IValidator<AnalyticsSummaryQuery> summaryValidator,
        IValidator<AnalyticsTimeSeriesQuery> timeSeriesValidator)
    {
        _analyticsQueryService = analyticsQueryService;
        _summaryValidator = summaryValidator;
        _timeSeriesValidator = timeSeriesValidator;
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(AnalyticsSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSummary(
        [FromRoute] string shortCode,
        [FromQuery] AnalyticsSummaryQuery query,
        CancellationToken ct)
    {
        EnsureValidModel();
        await _summaryValidator.ValidateAndThrowAsync(query, ct);

        var response = await _analyticsQueryService.GetSummaryAsync(query, shortCode, ct);
        return response is null
            ? NotFound(ApiErrorFactory.Create(HttpContext, "NOT_FOUND", "Short URL not found."))
            : Ok(response);
    }

    [HttpGet("time-series")]
    [ProducesResponseType(typeof(AnalyticsTimeSeriesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTimeSeries(
        [FromRoute] string shortCode,
        [FromQuery] AnalyticsTimeSeriesQuery query,
        CancellationToken ct)
    {
        EnsureValidModel();
        await _timeSeriesValidator.ValidateAndThrowAsync(query, ct);

        var response = await _analyticsQueryService.GetTimeSeriesAsync(query, shortCode, ct);
        return response is null
            ? NotFound(ApiErrorFactory.Create(HttpContext, "NOT_FOUND", "Short URL not found."))
            : Ok(response);
    }

    private void EnsureValidModel()
    {
        if (ModelState.IsValid)
        {
            return;
        }

        var failures = ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .SelectMany(x => x.Value!.Errors.Select(error =>
                new FluentValidation.Results.ValidationFailure(
                    string.IsNullOrWhiteSpace(x.Key) ? "query" : x.Key,
                    string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "The query value is invalid."
                        : error.ErrorMessage)))
            .ToList();

        throw new ValidationException(failures);
    }
}
