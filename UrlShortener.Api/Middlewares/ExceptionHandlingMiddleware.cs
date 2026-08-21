using System.Globalization;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Antiforgery;
using UrlShortener.Api.Models;
using UrlShortener.Api.Security;
using UrlShortener.Application.Exceptions;

namespace UrlShortener.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogDebug(
                "Request {TraceId} was cancelled before completion.",
                context.TraceIdentifier);
        }
        catch (Exception exception)
        {
            await HandleAsync(context, exception);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var statusCode = StatusCodes.Status500InternalServerError;
        var code = "UNEXPECTED_ERROR";
        var message = "Unexpected error.";
        var details = new List<ErrorDetail>();

        if (exception is ValidationException validationException)
        {
            statusCode = StatusCodes.Status400BadRequest;
            code = "VALIDATION_ERROR";
            message = "Validation failed.";
            details = validationException.Errors.Select(x => new ErrorDetail
            {
                Field = ApiErrorFactory.ToCamelCase(x.PropertyName),
                Message = x.ErrorMessage
            }).ToList();
        }
        else if (exception is Microsoft.AspNetCore.Http.BadHttpRequestException badRequestException &&
            badRequestException.StatusCode == StatusCodes.Status413PayloadTooLarge)
        {
            statusCode = StatusCodes.Status413PayloadTooLarge;
            code = "REQUEST_TOO_LARGE";
            message = "The request body exceeds the permitted size.";
        }
        else if (exception is AliasConflictException)
        {
            statusCode = StatusCodes.Status409Conflict;
            code = "ALIAS_CONFLICT";
            message = "Alias already exists.";
        }
        else if (exception is IdempotencyKeyReusedException)
        {
            statusCode = StatusCodes.Status409Conflict;
            code = "IDEMPOTENCY_KEY_REUSED";
            message = "The idempotency key was already used with different request content.";
        }
        else if (exception is NotFoundException)
        {
            statusCode = StatusCodes.Status404NotFound;
            code = "NOT_FOUND";
            message = "Resource not found.";
        }
        else if (exception is ExpiredException)
        {
            statusCode = StatusCodes.Status410Gone;
            code = "EXPIRED";
            message = "Short URL has expired.";
        }
        else if (exception is RestoreNotDeletedException)
        {
            statusCode = StatusCodes.Status409Conflict;
            code = "RESTORE_NOT_DELETED";
            message = "Short URL is not deleted.";
        }
        else if (exception is RestoreWindowExpiredException)
        {
            statusCode = StatusCodes.Status410Gone;
            code = "RESTORE_WINDOW_EXPIRED";
            message = "The short URL restore window has expired.";
        }
        else if (exception is RateLimitedException rateLimitedException)
        {
            statusCode = StatusCodes.Status429TooManyRequests;
            code = "RATE_LIMITED";
            message = rateLimitedException.Message;
            context.Response.Headers["Retry-After"] = rateLimitedException.RetryAfterSeconds.ToString(CultureInfo.InvariantCulture);
            context.Response.Headers.CacheControl = "no-store";
        }
        else if (exception is RateLimitingUnavailableException)
        {
            statusCode = StatusCodes.Status503ServiceUnavailable;
            code = "RATE_LIMITING_UNAVAILABLE";
            message = "Request rate limiting is temporarily unavailable.";
        }
        else if (exception is ShortCodeGenerationFailedException)
        {
            statusCode = StatusCodes.Status500InternalServerError;
            code = "SHORTCODE_GENERATION_FAILED";
            message = "Failed to generate short code.";
        }
        else if (exception is AccountUnavailableException)
        {
            statusCode = StatusCodes.Status409Conflict;
            code = "ACCOUNT_UNAVAILABLE";
            message = "An account cannot be created with the supplied identity.";
        }
        else if (exception is PasswordPolicyException)
        {
            statusCode = StatusCodes.Status400BadRequest;
            code = "VALIDATION_ERROR";
            message = "Validation failed.";
            details.Add(new ErrorDetail
            {
                Field = "password",
                Message = "Password does not satisfy the configured password policy."
            });
        }
        else if (exception is InvalidCredentialsException)
        {
            statusCode = StatusCodes.Status401Unauthorized;
            code = "AUTHENTICATION_FAILED";
            message = "Invalid credentials.";
        }
        else if (exception is InvalidSessionException)
        {
            statusCode = StatusCodes.Status401Unauthorized;
            code = "INVALID_SESSION";
            message = "Authentication session is invalid or expired.";
        }
        else if (exception is AuthenticatedUserRequiredException)
        {
            statusCode = StatusCodes.Status401Unauthorized;
            code = "AUTHENTICATION_REQUIRED";
            message = "A valid access token is required.";
        }
        else if (exception is AuthenticationPersistenceUnavailableException)
        {
            statusCode = StatusCodes.Status503ServiceUnavailable;
            code = "AUTHENTICATION_UNAVAILABLE";
            message = "Authentication is temporarily unavailable.";
        }
        else if (exception is ApiKeyLimitExceededException)
        {
            statusCode = StatusCodes.Status409Conflict;
            code = "API_KEY_LIMIT_REACHED";
            message = "The maximum number of active API keys has been reached.";
        }
        else if (exception is ApiKeyStateConflictException)
        {
            statusCode = StatusCodes.Status409Conflict;
            code = "API_KEY_STATE_CONFLICT";
            message = "The API key is not in a state that permits this operation.";
        }
        else if (exception is ApiKeyManagementUnavailableException)
        {
            statusCode = StatusCodes.Status503ServiceUnavailable;
            code = "API_KEY_MANAGEMENT_UNAVAILABLE";
            message = "API-key management requires SQL-backed persistence.";
        }
        else if (exception is CustomDomainConflictException)
        {
            statusCode = StatusCodes.Status409Conflict;
            code = "CUSTOM_DOMAIN_ALREADY_CLAIMED";
            message = "The normalized hostname is already registered.";
        }
        else if (exception is CustomDomainReservedException)
        {
            statusCode = StatusCodes.Status400BadRequest;
            code = "CUSTOM_DOMAIN_RESERVED";
            message = "The hostname belongs to a protected platform namespace and cannot be registered.";
        }
        else if (exception is CustomDomainStateConflictException)
        {
            statusCode = StatusCodes.Status409Conflict;
            code = "CUSTOM_DOMAIN_STATE_CONFLICT";
            message = "The custom domain changed state and does not permit this operation. Refresh it and try again.";
        }
        else if (exception is CustomDomainUnavailableException)
        {
            statusCode = StatusCodes.Status409Conflict;
            code = "CUSTOM_DOMAIN_UNAVAILABLE";
            message = "The selected custom domain is not owned, verified, and enabled.";
        }
        else if (exception is CsrfValidationException or AntiforgeryValidationException)
        {
            statusCode = StatusCodes.Status400BadRequest;
            code = "CSRF_VALIDATION_FAILED";
            message = "Request origin or antiforgery validation failed.";
        }

        if (code == "UNEXPECTED_ERROR")
        {
            _logger.LogError(
                "Unhandled exception of type {ExceptionType} while processing request {TraceId}. Exception details were suppressed to protect request secrets.",
                exception.GetType().FullName,
                context.TraceIdentifier);
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = ApiErrorFactory.Create(context, code, message, details);

        var json = JsonSerializer.Serialize(response, JsonOptions);
        await context.Response.WriteAsync(json, context.RequestAborted);
    }

}
