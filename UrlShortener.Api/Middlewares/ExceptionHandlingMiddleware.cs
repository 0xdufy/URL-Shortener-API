using System.Text.Json;
using FluentValidation;
using UrlShortener.Api.Models;
using UrlShortener.Application.Exceptions;

namespace UrlShortener.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleAsync(context, exception);
        }
    }

    private static async Task HandleAsync(HttpContext context, Exception exception)
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
            details = validationException.Errors
                .Select(x => new ErrorDetail
                {
                    Field = ToCamelCase(x.PropertyName),
                    Message = x.ErrorMessage
                })
                .ToList();
        }
        else if (exception is AliasConflictException)
        {
            statusCode = StatusCodes.Status409Conflict;
            code = "ALIAS_CONFLICT";
            message = "Alias already exists.";
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
        else if (exception is RateLimitedException rateLimitedException)
        {
            statusCode = StatusCodes.Status429TooManyRequests;
            code = "RATE_LIMITED";
            message = rateLimitedException.Message;
            context.Response.Headers["Retry-After"] = rateLimitedException.RetryAfterSeconds.ToString();
        }
        else if (exception is ShortCodeGenerationFailedException)
        {
            statusCode = StatusCodes.Status500InternalServerError;
            code = "SHORTCODE_GENERATION_FAILED";
            message = "Failed to generate short code.";
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse
        {
            TraceId = context.TraceIdentifier,
            Error = new ErrorBody
            {
                Code = code,
                Message = message,
                Details = details
            }
        };

        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }

    private static string ToCamelCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (value.Length == 1)
        {
            return value.ToLowerInvariant();
        }

        return char.ToLowerInvariant(value[0]) + value[1..];
    }
}
