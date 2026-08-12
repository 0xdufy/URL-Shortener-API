using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Models;
using UrlShortener.Application.Dtos;
using UrlShortener.Application.Interfaces;

namespace UrlShortener.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("r")]
public class RedirectController : ControllerBase
{
    private readonly IRedirectResolver _redirectResolver;
    private readonly ILogger<RedirectController> _logger;

    public RedirectController(
        IRedirectResolver redirectResolver,
        ILogger<RedirectController> logger)
    {
        _redirectResolver = redirectResolver;
        _logger = logger;
    }

    [HttpGet("{shortCode}")]
    public async Task<IActionResult> RedirectToOriginal([FromRoute] string shortCode, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers.UserAgent.ToString();
        var referer = Request.Headers.Referer.ToString();

        var startedAt = Stopwatch.GetTimestamp();
        var result = await _redirectResolver.ResolveAsync(
            shortCode,
            ip,
            string.IsNullOrWhiteSpace(userAgent) ? null : userAgent,
            string.IsNullOrWhiteSpace(referer) ? null : referer,
            ct);

        _logger.LogDebug(
            "Redirect resolution for {ShortCode} completed with {ResolutionStatus} from {ResolutionSource} in {ElapsedMilliseconds} ms.",
            shortCode,
            result.Status,
            result.Source,
            Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);

        if (result.Status == RedirectResolutionStatus.Resolved && result.OriginalUrl != null)
        {
            return Redirect(result.OriginalUrl);
        }

        if (result.Status == RedirectResolutionStatus.Expired)
        {
            return StatusCode(StatusCodes.Status410Gone, ApiErrorFactory.Create(HttpContext, "EXPIRED", "Short URL has expired."));
        }

        return NotFound(ApiErrorFactory.Create(HttpContext, "NOT_FOUND", "Short URL not found."));
    }
}
