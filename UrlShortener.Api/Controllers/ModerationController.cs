using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Models;
using UrlShortener.Api.RateLimiting;
using UrlShortener.Application.Dtos;
using UrlShortener.Application.Interfaces;
using UrlShortener.Application.RateLimiting;
using UrlShortener.Application.Security;

namespace UrlShortener.Api.Controllers;

[ApiController]
[Route("api/v1/moderation/short-urls")]
[Authorize(Policy = AuthorizationRoles.Moderator)]
[DistributedRateLimit(RateLimitPolicy.Authenticated)]
public sealed class ModerationController : ControllerBase
{
    private readonly IShortUrlModerationService _moderationService;
    private readonly IValidator<ModerateShortUrlRequest> _validator;

    public ModerationController(
        IShortUrlModerationService moderationService,
        IValidator<ModerateShortUrlRequest> validator)
    {
        _moderationService = moderationService;
        _validator = validator;
    }

    [HttpPut("{shortUrlId:guid}")]
    [Consumes("application/json")]
    [RequestSizeLimit(4 * 1024)]
    [ProducesResponseType(typeof(ShortUrlModerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShortUrlModerationResponse>> Moderate(
        Guid shortUrlId,
        [FromBody] ModerateShortUrlRequest request,
        CancellationToken ct)
    {
        await _validator.ValidateAndThrowAsync(request, ct);
        var response = await _moderationService.ModerateAsync(shortUrlId, request, ct);
        return response is null
            ? NotFound(ApiErrorFactory.Create(HttpContext, "NOT_FOUND", "Short URL not found."))
            : Ok(response);
    }
}
