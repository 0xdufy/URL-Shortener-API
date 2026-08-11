using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Models;
using UrlShortener.Application.Dtos;
using UrlShortener.Application.Interfaces;

namespace UrlShortener.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/short-urls")]
public class ShortUrlsController : ControllerBase
{
    private readonly IShortUrlService _shortUrlService;
    private readonly IRateLimiter _rateLimiter;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<CreateShortUrlRequest> _createValidator;
    private readonly IValidator<UpdateStatusRequest> _updateStatusValidator;
    private readonly IValidator<ShortUrlListQuery> _listValidator;

    public ShortUrlsController(
        IShortUrlService shortUrlService,
        IRateLimiter rateLimiter,
        IDateTimeProvider dateTimeProvider,
        IValidator<CreateShortUrlRequest> createValidator,
        IValidator<UpdateStatusRequest> updateStatusValidator,
        IValidator<ShortUrlListQuery> listValidator)
    {
        _shortUrlService = shortUrlService;
        _rateLimiter = rateLimiter;
        _dateTimeProvider = dateTimeProvider;
        _createValidator = createValidator;
        _updateStatusValidator = updateStatusValidator;
        _listValidator = listValidator;
    }

    /// <summary>Lists short URLs owned by the authenticated user.</summary>
    /// <remarks>
    /// Defaults to page 1 with 20 items and excludes soft-deleted links. PageSize is capped at 100.
    /// Expiration accepts all, expired, or notExpired. SortBy accepts createdAt, shortCode,
    /// clickCount, or expiresAt; sortDirection accepts asc or desc.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(ShortUrlListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ShortUrlListResponse>> List([FromQuery] ShortUrlListQuery query, CancellationToken ct)
    {
        EnsureValidModel(query);
        await _listValidator.ValidateAndThrowAsync(query, ct);

        return Ok(await _shortUrlService.ListAsync(query, ct));
    }

    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ShortUrlResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] CreateShortUrlRequest? request, CancellationToken ct)
    {
        var ip = GetClientIp();
        var allowed = _rateLimiter.IsAllowed(ip, _dateTimeProvider.UtcNow, out var remaining, out var retryAfterSeconds);
        if (!allowed)
        {
            Response.Headers["Retry-After"] = retryAfterSeconds.ToString();
            return StatusCode(StatusCodes.Status429TooManyRequests, CreateError("RATE_LIMITED", $"Too many requests. Retry after {retryAfterSeconds} seconds.", new List<ErrorDetail>()));
        }

        var validRequest = EnsureValidModelAndBody(request);

        await _createValidator.ValidateAndThrowAsync(validRequest, ct);

        var baseHost = $"{Request.Scheme}://{Request.Host}";
        var response = await _shortUrlService.CreateAsync(validRequest, baseHost, ip, ct);

        return Created($"/api/v1/short-urls/{response.ShortCode}", response);
    }

    [HttpGet("{shortCode}")]
    public async Task<IActionResult> GetByShortCode([FromRoute] string shortCode, CancellationToken ct)
    {
        var response = await _shortUrlService.GetAsync(shortCode, ct);
        if (response == null)
        {
            return NotFound(CreateError("NOT_FOUND", "Short URL not found.", new List<ErrorDetail>()));
        }

        return Ok(response);
    }

    [HttpPatch("{shortCode}/status")]
    public async Task<IActionResult> UpdateStatus([FromRoute] string shortCode, [FromBody] UpdateStatusRequest? request, CancellationToken ct)
    {
        var validRequest = EnsureValidModelAndBody(request);

        await _updateStatusValidator.ValidateAndThrowAsync(validRequest, ct);

        var response = await _shortUrlService.SetStatusAsync(shortCode, validRequest.IsActive, ct);
        if (response == null)
        {
            return NotFound(CreateError("NOT_FOUND", "Short URL not found.", new List<ErrorDetail>()));
        }

        return Ok(response);
    }

    [HttpDelete("{shortCode}")]
    public async Task<IActionResult> Delete([FromRoute] string shortCode, CancellationToken ct)
    {
        var deleted = await _shortUrlService.DeleteAsync(shortCode, ct);
        if (!deleted)
        {
            return NotFound(CreateError("NOT_FOUND", "Short URL not found.", new List<ErrorDetail>()));
        }

        return NoContent();
    }

    [HttpGet("{shortCode}/stats")]
    public async Task<IActionResult> GetStats([FromRoute] string shortCode, [FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, CancellationToken ct)
    {
        var response = await _shortUrlService.GetStatsAsync(shortCode, fromUtc, toUtc, ct);
        if (response == null)
        {
            return NotFound(CreateError("NOT_FOUND", "Short URL not found.", new List<ErrorDetail>()));
        }

        return Ok(response);
    }

    private string GetClientIp()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private T EnsureValidModelAndBody<T>(T? request) where T : class
    {
        EnsureValidModel(request);

        if (request == null)
        {
            throw new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure("request", "Request body is required.")
            });
        }

        return request;
    }

    private void EnsureValidModel(object? request)
    {
        if (!ModelState.IsValid)
        {
            var failures = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e =>
                    new FluentValidation.Results.ValidationFailure(
                        string.IsNullOrWhiteSpace(x.Key) ? "request" : x.Key,
                        string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Invalid request body." : e.ErrorMessage)))
                .ToList();

            throw new ValidationException(failures);
        }
    }

    private ErrorResponse CreateError(string code, string message, List<ErrorDetail> details)
    {
        return new ErrorResponse
        {
            TraceId = HttpContext.TraceIdentifier,
            Error = new ErrorBody
            {
                Code = code,
                Message = message,
                Details = details
            }
        };
    }
}
