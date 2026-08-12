using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using UrlShortener.Api.Middlewares;
using UrlShortener.Api.Models;
using UrlShortener.Api.Security;
using UrlShortener.Application.Authentication;
using UrlShortener.Application.Dtos;
using UrlShortener.Application.Exceptions;
using UrlShortener.Application.Interfaces;
using UrlShortener.Infrastructure.Configuration;

namespace UrlShortener.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class AuthenticationController : ControllerBase
{
    public const string RefreshCookieName = "urlshortener.refresh";

    private readonly IAuthenticationService _authenticationService;
    private readonly IAuthenticationRateLimiter _rateLimiter;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<SignInRequest> _signInValidator;
    private readonly IAntiforgery _antiforgery;
    private readonly IdentitySecurityOptions _options;

    public AuthenticationController(
        IAuthenticationService authenticationService,
        IAuthenticationRateLimiter rateLimiter,
        IDateTimeProvider dateTimeProvider,
        IValidator<RegisterRequest> registerValidator,
        IValidator<SignInRequest> signInValidator,
        IAntiforgery antiforgery,
        IOptions<IdentitySecurityOptions> options)
    {
        _authenticationService = authenticationService;
        _rateLimiter = rateLimiter;
        _dateTimeProvider = dateTimeProvider;
        _registerValidator = registerValidator;
        _signInValidator = signInValidator;
        _antiforgery = antiforgery;
        _options = options.Value;
    }

    [HttpGet("bootstrap")]
    [ProducesResponseType(typeof(BrowserAuthenticationBootstrapResponse), StatusCodes.Status200OK)]
    public ActionResult<BrowserAuthenticationBootstrapResponse> Bootstrap()
    {
        var csrfTokens = _antiforgery.GetAndStoreTokens(HttpContext);
        var csrfToken = csrfTokens.RequestToken ??
            throw new InvalidOperationException("Antiforgery token generation failed.");

        return Ok(new BrowserAuthenticationBootstrapResponse
        {
            CsrfToken = csrfToken,
            PublicRegistrationEnabled = _options.PublicRegistrationEnabled,
            PasswordRequiredLength = _options.PasswordRequiredLength,
            PasswordRequiredUniqueChars = _options.PasswordRequiredUniqueChars
        });
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthenticationSessionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<AuthenticationSessionResponse>> Register(
        [FromBody] RegisterRequest? request,
        CancellationToken cancellationToken)
    {
        EnsureAllowed(AuthenticationOperation.Register);
        var validRequest = EnsureValidBody(request);
        await _registerValidator.ValidateAndThrowAsync(validRequest, cancellationToken);
        var session = await _authenticationService.RegisterAsync(validRequest, cancellationToken);
        var response = WriteSession(session);
        return Created("/api/v1/auth/me", response);
    }

    [HttpPost("sign-in")]
    [ProducesResponseType(typeof(AuthenticationSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<AuthenticationSessionResponse>> SignIn(
        [FromBody] SignInRequest? request,
        CancellationToken cancellationToken)
    {
        EnsureAllowed(AuthenticationOperation.SignIn);
        var validRequest = EnsureValidBody(request);
        await _signInValidator.ValidateAndThrowAsync(validRequest, cancellationToken);
        var session = await _authenticationService.SignInAsync(validRequest, cancellationToken);
        return Ok(WriteSession(session));
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthenticationSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<AuthenticationSessionResponse>> Refresh(CancellationToken cancellationToken)
    {
        await ValidateBrowserMutationAsync();
        EnsureAllowed(AuthenticationOperation.Refresh);

        if (!Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken))
        {
            throw new InvalidSessionException();
        }

        var session = await _authenticationService.RefreshAsync(refreshToken, cancellationToken);
        return Ok(WriteSession(session));
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(CurrentAuthenticationSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<CurrentAuthenticationSessionResponse>> GetCurrent(
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var sessionIdValue = User.FindFirstValue(JwtAccessTokenIssuer.SessionIdClaim);
        if (!Guid.TryParse(userIdValue, out var userId) || !Guid.TryParse(sessionIdValue, out var sessionId))
        {
            throw new InvalidSessionException();
        }

        var current = await _authenticationService.GetCurrentAsync(userId, sessionId, cancellationToken);
        return Ok(new CurrentAuthenticationSessionResponse
        {
            SessionId = current.SessionId,
            RefreshSessionCreatedAtUtc = current.RefreshSessionCreatedAtUtc,
            RefreshSessionExpiresAtUtc = current.RefreshSessionExpiresAtUtc,
            IsRefreshSessionRevoked = current.IsRefreshSessionRevoked,
            User = MapUser(current.User)
        });
    }

    [HttpPost("sign-out")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> SignOut(CancellationToken cancellationToken)
    {
        await ValidateBrowserMutationAsync();
        Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken);
        await _authenticationService.SignOutAsync(refreshToken, cancellationToken);
        Response.Cookies.Delete(RefreshCookieName, CreateRefreshCookieOptions(null));
        return NoContent();
    }

    private AuthenticationSessionResponse WriteSession(IssuedAuthenticationSession session)
    {
        Response.Cookies.Append(
            RefreshCookieName,
            session.RefreshToken,
            CreateRefreshCookieOptions(session.RefreshSessionExpiresAtUtc));

        var csrfTokens = _antiforgery.GetAndStoreTokens(HttpContext);
        var csrfToken = csrfTokens.RequestToken ?? throw new InvalidOperationException("Antiforgery token generation failed.");

        return new AuthenticationSessionResponse
        {
            AccessToken = session.AccessToken,
            AccessTokenExpiresAtUtc = session.AccessTokenExpiresAtUtc,
            RefreshSessionExpiresAtUtc = session.RefreshSessionExpiresAtUtc,
            CsrfToken = csrfToken,
            User = MapUser(session.User)
        };
    }

    private async Task ValidateBrowserMutationAsync()
    {
        var origin = Request.Headers.Origin.ToString();
        if (string.IsNullOrWhiteSpace(origin) ||
            !_options.AllowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
        {
            throw new CsrfValidationException();
        }

        await _antiforgery.ValidateRequestAsync(HttpContext);
    }

    private void EnsureAllowed(AuthenticationOperation operation)
    {
        var partitionKey = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var decision = _rateLimiter.Check(operation, partitionKey, _dateTimeProvider.UtcNow);
        if (!decision.IsAllowed)
        {
            throw new RateLimitedException(
                $"Too many authentication requests. Retry after {decision.RetryAfterSeconds} seconds.",
                decision.RetryAfterSeconds);
        }
    }

    private CookieOptions CreateRefreshCookieOptions(DateTime? expiresAtUtc) => new()
    {
        HttpOnly = true,
        Secure = _options.RequireSecureCookies,
        SameSite = SameSiteMode.Strict,
        Path = "/api/v1/auth",
        IsEssential = true,
        Expires = expiresAtUtc.HasValue ? new DateTimeOffset(expiresAtUtc.Value, TimeSpan.Zero) : null
    };

    private T EnsureValidBody<T>(T? request) where T : class
    {
        if (!ModelState.IsValid)
        {
            var failures = ModelState
                .Where(item => item.Value?.Errors.Count > 0)
                .SelectMany(item => item.Value!.Errors.Select(error =>
                    new FluentValidation.Results.ValidationFailure(
                        string.IsNullOrWhiteSpace(item.Key) ? "request" : item.Key,
                        string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Invalid request body." : error.ErrorMessage)))
                .ToList();

            throw new ValidationException(failures);
        }

        if (request is null)
        {
            throw new ValidationException(
            [
                new FluentValidation.Results.ValidationFailure("request", "Request body is required.")
            ]);
        }

        return request;
    }

    private static AuthenticatedUserResponse MapUser(AuthenticatedUser user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        CreatedAtUtc = user.CreatedAtUtc
    };
}
