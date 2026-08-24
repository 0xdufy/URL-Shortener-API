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
[DistributedRateLimit(RateLimitPolicy.Authenticated)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
[Route("api/v1/short-urls")]
public class ShortUrlsController : ControllerBase
{
    private const int CreateRequestBodyLimitBytes = 8 * 1024;
    private static readonly HashSet<string> QrCodeQueryKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "size",
        "format",
        "errorCorrection",
        "foreground",
        "background"
    };

    private readonly IShortUrlService _shortUrlService;
    private readonly IQrCodeGenerator _qrCodeGenerator;
    private readonly IValidator<CreateShortUrlRequest> _createValidator;
    private readonly IValidator<UpdateShortUrlRequest> _updateValidator;
    private readonly IValidator<UpdateStatusRequest> _updateStatusValidator;
    private readonly IValidator<ShortUrlListQuery> _listValidator;

    public ShortUrlsController(
        IShortUrlService shortUrlService,
        IQrCodeGenerator qrCodeGenerator,
        IValidator<CreateShortUrlRequest> createValidator,
        IValidator<UpdateShortUrlRequest> updateValidator,
        IValidator<UpdateStatusRequest> updateStatusValidator,
        IValidator<ShortUrlListQuery> listValidator)
    {
        _shortUrlService = shortUrlService;
        _qrCodeGenerator = qrCodeGenerator;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
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
    [Authorize(Policy = ApiKeyAuthorizationPolicies.ShortUrlsRead)]
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
    [Authorize(Policy = ApiKeyAuthorizationPolicies.ShortUrlsCreate)]
    [RequestSizeLimit(CreateRequestBodyLimitBytes)]
    [DistributedRateLimit(RateLimitPolicy.UrlCreation)]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ShortUrlResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status504GatewayTimeout)]
    public async Task<IActionResult> Create(
        [FromBody] CreateShortUrlRequest? request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        var validRequest = EnsureValidModelAndBody(request);
        var validIdempotencyKey = ValidateIdempotencyKey(idempotencyKey);

        await _createValidator.ValidateAndThrowAsync(validRequest, ct);

        var response = await _shortUrlService.CreateAsync(validRequest, validIdempotencyKey, ct);

        return Created($"/api/v1/short-urls/{response.ShortCode}", response);
    }

    [HttpGet("{shortCode}")]
    [Authorize(Policy = ApiKeyAuthorizationPolicies.ShortUrlsRead)]
    [ProducesResponseType(typeof(ShortUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByShortCode([FromRoute] string shortCode, CancellationToken ct)
    {
        var response = await _shortUrlService.GetAsync(shortCode, ct);
        if (response == null)
        {
            return NotFound(ApiErrorFactory.Create(HttpContext, "NOT_FOUND", "Short URL not found."));
        }

        return Ok(response);
    }

    /// <summary>Generates an SVG QR code for an owned link's canonical short URL.</summary>
    /// <remarks>
    /// Size is limited to 128-1024 pixels. Format currently accepts svg. ErrorCorrection accepts
    /// low, medium, quartile, or high. Foreground and background accept high-contrast six-digit hex colors.
    /// The URL payload is resolved exclusively from the owned short-link resource.
    /// </remarks>
    [HttpGet("{shortCode}/qr-code")]
    [Authorize(Policy = ApiKeyAuthorizationPolicies.ShortUrlsRead)]
    [Produces("image/svg+xml")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "image/svg+xml")]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQrCode(
        [FromRoute] string shortCode,
        [FromQuery] QrCodeRequest request,
        [FromServices] IValidator<QrCodeRequest> validator,
        CancellationToken ct)
    {
        EnsureValidModel(request);
        EnsureSupportedQrCodeQuery();
        await validator.ValidateAndThrowAsync(request, ct);

        var link = await _shortUrlService.GetAsync(shortCode, ct);
        if (link == null)
        {
            return NotFound(ApiErrorFactory.Create(HttpContext, "NOT_FOUND", "Short URL not found."));
        }

        var document = _qrCodeGenerator.Generate(link.ShortUrl, request);
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(document.Content, document.ContentType, $"{link.ShortCode}-qr.{document.FileExtension}");
    }

    [HttpPut("{shortCode}")]
    [Authorize(Policy = ApiKeyAuthorizationPolicies.ShortUrlsWrite)]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ShortUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        [FromRoute] string shortCode,
        [FromBody] UpdateShortUrlRequest? request,
        CancellationToken ct)
    {
        var validRequest = EnsureValidModelAndBody(request);
        await _updateValidator.ValidateAndThrowAsync(validRequest, ct);

        var response = await _shortUrlService.UpdateAsync(shortCode, validRequest, ct);
        if (response == null)
        {
            return NotFound(ApiErrorFactory.Create(HttpContext, "NOT_FOUND", "Short URL not found."));
        }

        return Ok(response);
    }

    [HttpPatch("{shortCode}/status")]
    [Authorize(Policy = ApiKeyAuthorizationPolicies.ShortUrlsWrite)]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ShortUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus([FromRoute] string shortCode, [FromBody] UpdateStatusRequest? request, CancellationToken ct)
    {
        var validRequest = EnsureValidModelAndBody(request);

        await _updateStatusValidator.ValidateAndThrowAsync(validRequest, ct);

        var response = await _shortUrlService.SetStatusAsync(shortCode, validRequest.IsActive.GetValueOrDefault(), ct);
        if (response == null)
        {
            return NotFound(ApiErrorFactory.Create(HttpContext, "NOT_FOUND", "Short URL not found."));
        }

        return Ok(response);
    }

    [HttpDelete("{shortCode}")]
    [Authorize(Policy = ApiKeyAuthorizationPolicies.ShortUrlsWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] string shortCode, CancellationToken ct)
    {
        var deleted = await _shortUrlService.DeleteAsync(shortCode, ct);
        if (!deleted)
        {
            return NotFound(ApiErrorFactory.Create(HttpContext, "NOT_FOUND", "Short URL not found."));
        }

        return NoContent();
    }

    [HttpPost("{shortCode}/restore")]
    [Authorize(Policy = ApiKeyAuthorizationPolicies.ShortUrlsWrite)]
    [ProducesResponseType(typeof(ShortUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status410Gone)]
    public async Task<IActionResult> Restore([FromRoute] string shortCode, CancellationToken ct)
    {
        return Ok(await _shortUrlService.RestoreAsync(shortCode, ct));
    }

    [HttpGet("{shortCode}/stats")]
    [Authorize(Policy = ApiKeyAuthorizationPolicies.AnalyticsRead)]
    [ProducesResponseType(typeof(StatsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStats([FromRoute] string shortCode, [FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, CancellationToken ct)
    {
        var response = await _shortUrlService.GetStatsAsync(shortCode, fromUtc, toUtc, ct);
        if (response == null)
        {
            return NotFound(ApiErrorFactory.Create(HttpContext, "NOT_FOUND", "Short URL not found."));
        }

        return Ok(response);
    }

    private void EnsureSupportedQrCodeQuery()
    {
        var unsupported = Request.Query.Keys
            .Where(key => !QrCodeQueryKeys.Contains(key))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (unsupported.Length == 0)
        {
            return;
        }

        throw new ValidationException(unsupported.Select(key =>
            new FluentValidation.Results.ValidationFailure(
                key,
                $"Unsupported QR code option '{key}'.")));
    }

    private static string? ValidateIdempotencyKey(string? value)
    {
        if (value == null)
        {
            return null;
        }

        if (value.Length is < 16 or > 128 || !value.All(IsIdempotencyKeyCharacter))
        {
            throw new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure(
                    "idempotencyKey",
                    "Idempotency-Key must be one 16-128 character value containing only ASCII letters, digits, '.', '_', ':', or '-'.")
            });
        }

        return value;
    }

    private static bool IsIdempotencyKeyCharacter(char value) =>
        value is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '.' or '_' or ':' or '-';

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

}
