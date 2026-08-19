using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Models;
using UrlShortener.Api.RateLimiting;
using UrlShortener.Application.Dtos;
using UrlShortener.Application.Interfaces;
using UrlShortener.Application.RateLimiting;

namespace UrlShortener.Api.Controllers;

[ApiController]
[Authorize]
[DistributedRateLimit(RateLimitPolicy.Authenticated)]
[Route("api/v1/api-keys")]
[Produces("application/json")]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
public sealed class ApiKeysController : ControllerBase
{
    private const int CreateRequestBodyLimitBytes = 4 * 1024;

    private readonly IApiKeyService _apiKeyService;
    private readonly IValidator<CreateApiKeyRequest> _createValidator;

    public ApiKeysController(
        IApiKeyService apiKeyService,
        IValidator<CreateApiKeyRequest> createValidator)
    {
        _apiKeyService = apiKeyService;
        _createValidator = createValidator;
    }

    /// <summary>Creates an owned API key and reveals its full credential exactly once.</summary>
    [HttpPost]
    [RequestSizeLimit(CreateRequestBodyLimitBytes)]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiKeyCreationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateApiKeyRequest? request, CancellationToken ct)
    {
        var validRequest = RequireBody(request);
        await _createValidator.ValidateAndThrowAsync(validRequest, ct);
        var response = await _apiKeyService.CreateAsync(validRequest, ct);
        Response.Headers.CacheControl = "no-store";
        return Created($"/api/v1/api-keys/{response.ApiKey.Id}", response);
    }

    /// <summary>Lists safe metadata for every API key owned by the current user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ApiKeyResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ApiKeyResponse>>> List(CancellationToken ct)
    {
        return Ok(await _apiKeyService.ListAsync(ct));
    }

    /// <summary>Revokes an owned API key while retaining its audit metadata.</summary>
    [HttpDelete("{apiKeyId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Revoke([FromRoute] Guid apiKeyId, CancellationToken ct)
    {
        await _apiKeyService.RevokeAsync(apiKeyId, ct);
        return NoContent();
    }

    /// <summary>Revokes an active key and creates a linked replacement with the same name, scopes, and expiry.</summary>
    [HttpPost("{apiKeyId:guid}/rotate")]
    [ProducesResponseType(typeof(ApiKeyCreationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Rotate([FromRoute] Guid apiKeyId, CancellationToken ct)
    {
        var response = await _apiKeyService.RotateAsync(apiKeyId, ct);
        Response.Headers.CacheControl = "no-store";
        return Created($"/api/v1/api-keys/{response.ApiKey.Id}", response);
    }

    private T RequireBody<T>(T? request) where T : class
    {
        if (!ModelState.IsValid)
        {
            var failures = ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .SelectMany(entry => entry.Value!.Errors.Select(error =>
                    new FluentValidation.Results.ValidationFailure(
                        string.IsNullOrWhiteSpace(entry.Key) ? "request" : entry.Key,
                        string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Invalid request body." : error.ErrorMessage)));
            throw new ValidationException(failures);
        }

        if (request == null)
        {
            throw new ValidationException([
                new FluentValidation.Results.ValidationFailure("request", "Request body is required.")
            ]);
        }

        return request;
    }
}
