using FluentValidation.Results;

namespace UrlShortener.Api.Models;

public static class ApiErrorFactory
{
    public static ErrorResponse Create(
        HttpContext context,
        string code,
        string message,
        IEnumerable<ErrorDetail>? details = null)
    {
        return new ErrorResponse
        {
            TraceId = context.TraceIdentifier,
            Error = new ErrorBody
            {
                Code = code,
                Message = message,
                Details = details?.ToList() ?? []
            }
        };
    }

    public static ErrorResponse Validation(HttpContext context, IEnumerable<ValidationFailure> failures)
    {
        var details = failures.Select(failure => new ErrorDetail
        {
            Field = ToCamelCase(failure.PropertyName),
            Message = failure.ErrorMessage
        });

        return Create(context, "VALIDATION_ERROR", "Validation failed.", details);
    }

    public static string ToCamelCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "request";
        }

        if (value.Length == 1)
        {
            return value.ToLowerInvariant();
        }

        return char.ToLowerInvariant(value[0]) + value[1..];
    }
}
