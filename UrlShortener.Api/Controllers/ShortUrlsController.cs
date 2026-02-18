using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Models;
using UrlShortener.Application.Dtos;
using UrlShortener.Application.Interfaces;

namespace UrlShortener.Api.Controllers;

[ApiController]
[Route("api/v1/short-urls")]
public class ShortUrlsController : ControllerBase
{
    private readonly IShortUrlService _shortUrlService;
    private readonly IRateLimiter _rateLimiter;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<CreateShortUrlRequest> _createValidator;
    private readonly IValidator<UpdateStatusRequest> _updateStatusValidator;

    public ShortUrlsController(
        IShortUrlService shortUrlService,
        IRateLimiter rateLimiter,
        IDateTimeProvider dateTimeProvider,
        IValidator<CreateShortUrlRequest> createValidator,
        IValidator<UpdateStatusRequest> updateStatusValidator)
    {
        _shortUrlService = shortUrlService;
        _rateLimiter = rateLimiter;
        _dateTimeProvider = dateTimeProvider;
        _createValidator = createValidator;
        _updateStatusValidator = updateStatusValidator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateShortUrlRequest? request, CancellationToken ct)
    {
        var ip = GetClientIp();
        var allowed = _rateLimiter.IsAllowed(ip, _dateTimeProvider.UtcNow, out var remaining, out var retryAfterSeconds);
        if (!allowed)
        {
            Response.Headers["Retry-After"] = retryAfterSeconds.ToString();
            return StatusCode(StatusCodes.Status429TooManyRequests, CreateError("RATE_LIMITED", $"Too many requests. Retry after {retryAfterSeconds} seconds.", new List<ErrorDetail>()));
        }

        if (request == null)
        {
            throw new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure("request", "Request body is required.")
            });
        }

        await _createValidator.ValidateAndThrowAsync(request, ct);

        var baseHost = $"{Request.Scheme}://{Request.Host}";
        var response = await _shortUrlService.CreateAsync(request, baseHost, ip, ct);

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
        if (request == null)
        {
            throw new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure("request", "Request body is required.")
            });
        }

        await _updateStatusValidator.ValidateAndThrowAsync(request, ct);

        var response = await _shortUrlService.SetStatusAsync(shortCode, request.IsActive, ct);
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
