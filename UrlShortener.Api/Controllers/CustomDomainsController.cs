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
[Route("api/v1/custom-domains")]
[Produces("application/json")]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
public sealed class CustomDomainsController : ControllerBase
{
    private const int RequestBodyLimitBytes = 4 * 1024;

    private readonly ICustomDomainService _service;
    private readonly IValidator<RegisterCustomDomainRequest> _registerValidator;

    public CustomDomainsController(
        ICustomDomainService service,
        IValidator<RegisterCustomDomainRequest> registerValidator)
    {
        _service = service;
        _registerValidator = registerValidator;
    }

    /// <summary>Registers a globally unique normalized hostname in the pending state.</summary>
    [HttpPost]
    [RequestSizeLimit(RequestBodyLimitBytes)]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(CustomDomainResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterCustomDomainRequest? request,
        CancellationToken ct)
    {
        var validRequest = RequireBody(request);
        await _registerValidator.ValidateAndThrowAsync(validRequest, ct);
        var response = await _service.RegisterAsync(validRequest, ct);
        SetSensitiveResponseHeaders();
        return Created($"/api/v1/custom-domains/{response.Id}", response);
    }

    /// <summary>Lists custom domains owned by the authenticated user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CustomDomainResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CustomDomainResponse>>> List(CancellationToken ct)
    {
        SetSensitiveResponseHeaders();
        return Ok(await _service.ListAsync(ct));
    }

    /// <summary>Rotates the DNS token and returns the domain to pending verification.</summary>
    [HttpPost("{customDomainId:guid}/verification/request")]
    [ProducesResponseType(typeof(CustomDomainResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CustomDomainResponse>> RequestVerification(
        [FromRoute] Guid customDomainId,
        CancellationToken ct)
    {
        SetSensitiveResponseHeaders();
        return Ok(await _service.RequestVerificationAsync(customDomainId, ct));
    }

    /// <summary>Checks the authoritative external DNS TXT evidence for the current token.</summary>
    [HttpPost("{customDomainId:guid}/verification/check")]
    [ProducesResponseType(typeof(CustomDomainResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CustomDomainResponse>> CheckVerification(
        [FromRoute] Guid customDomainId,
        CancellationToken ct)
    {
        SetSensitiveResponseHeaders();
        return Ok(await _service.CheckVerificationAsync(customDomainId, ct));
    }

    /// <summary>Disables the domain so it cannot serve branded links.</summary>
    [HttpPost("{customDomainId:guid}/disable")]
    [ProducesResponseType(typeof(CustomDomainResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomDomainResponse>> Disable(
        [FromRoute] Guid customDomainId,
        CancellationToken ct)
    {
        SetSensitiveResponseHeaders();
        return Ok(await _service.DisableAsync(customDomainId, ct));
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

    private void SetSensitiveResponseHeaders() => Response.Headers.CacheControl = "no-store";
}
