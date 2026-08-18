using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using UrlShortener.Api.Controllers;

namespace UrlShortener.Api.OpenApi;

public sealed class AnalyticsOperationFilter : IOperationFilter
{
    private static readonly IReadOnlyDictionary<string, string> CommonParameterDescriptions =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["shortCode"] = "Case-sensitive short code for a non-deleted link owned by the authenticated user.",
            ["fromUtc"] = "Inclusive UTC bucket boundary. Supply together with toUtc, using Z or +00:00.",
            ["toUtc"] = "Exclusive UTC bucket boundary. Supply together with fromUtc, using Z or +00:00."
        };

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(AnalyticsController))
        {
            return;
        }

        if (context.MethodInfo.Name == nameof(AnalyticsController.GetSummary))
        {
            operation.Summary = "Get owned-link analytics summary";
            operation.Description =
                "Returns daily aggregate totals, a sum of privacy-preserving daily unique-visitor estimates, " +
                "top referrers, and complete device/browser/OS category breakdowns. Boundaries must be UTC midnight, " +
                "the range is limited to 366 days, and omission defaults to the current plus preceding 29 UTC days.";
        }
        else if (context.MethodInfo.Name == nameof(AnalyticsController.GetTimeSeries))
        {
            operation.Summary = "Get owned-link analytics time series";
            operation.Description =
                "Returns ordered, zero-filled UTC buckets from indexed aggregates. Hour supports at most 744 buckets " +
                "and day supports at most 731 buckets. Omission defaults to 24 hourly or 30 daily buckets.";
        }
        else
        {
            return;
        }

        foreach (var parameter in operation.Parameters?.OfType<OpenApiParameter>() ?? [])
        {
            if (parameter.Name is null)
            {
                continue;
            }

            if (CommonParameterDescriptions.TryGetValue(parameter.Name, out var description))
            {
                parameter.Description = description;
            }
            else if (parameter.Name.Equals("topReferrers", StringComparison.OrdinalIgnoreCase))
            {
                parameter.Description = "Number of leading referrer/source categories to return. Defaults to 10; maximum 20.";
            }
            else if (parameter.Name.Equals("granularity", StringComparison.OrdinalIgnoreCase))
            {
                parameter.Description = "Approved bucket granularity: hour or day. Defaults to day.";
            }

            parameter.Name = char.ToLowerInvariant(parameter.Name[0]) + parameter.Name[1..];
        }
    }
}
