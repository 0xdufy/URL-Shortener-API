using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Models;
using UrlShortener.Application.Interfaces;

namespace UrlShortener.Api.Controllers;

[ApiController]
[Route("r")]
public class RedirectController : ControllerBase
{
    private readonly IShortUrlService _shortUrlService;

    public RedirectController(IShortUrlService shortUrlService)
    {
        _shortUrlService = shortUrlService;
    }

    [HttpGet("{shortCode}")]
    public async Task<IActionResult> RedirectToOriginal([FromRoute] string shortCode, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers.UserAgent.ToString();
        var referer = Request.Headers.Referer.ToString();

        var result = await _shortUrlService.ResolveForRedirectAsync(shortCode, ip, string.IsNullOrWhiteSpace(userAgent) ? null : userAgent, string.IsNullOrWhiteSpace(referer) ? null : referer, ct);

        if (result.statusCode == StatusCodes.Status302Found && result.originalUrl != null)
        {
            return Redirect(result.originalUrl);
        }

        if (result.statusCode == StatusCodes.Status410Gone)
        {
            return StatusCode(StatusCodes.Status410Gone, CreateError("EXPIRED", "Short URL has expired."));
        }

        return NotFound(CreateError("NOT_FOUND", "Short URL not found."));
    }

    private ErrorResponse CreateError(string code, string message)
    {
        return new ErrorResponse
        {
            TraceId = HttpContext.TraceIdentifier,
            Error = new ErrorBody
            {
                Code = code,
                Message = message,
                Details = new List<ErrorDetail>()
            }
        };
    }
}
