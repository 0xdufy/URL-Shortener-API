using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using UrlShortener.Api.Controllers;

namespace UrlShortener.Api.OpenApi;

public sealed class OwnedShortUrlListOperationFilter : IOperationFilter
{
    private static readonly IReadOnlyDictionary<string, string> ParameterDescriptions =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["page"] = "One-based page number. Defaults to 1.",
            ["pageSize"] = "Items per page. Defaults to 20; maximum 100.",
            ["search"] = "Contains search using case-sensitive short-code matching plus the destination URL. Maximum 200 characters.",
            ["isActive"] = "When supplied, returns only active or inactive links.",
            ["expiration"] = "Expiration filter: all, expired, or notExpired. Defaults to all.",
            ["includeDeleted"] = "Includes soft-deleted links when true. Defaults to false.",
            ["createdFromUtc"] = "Inclusive creation timestamp lower bound. Must use Z or +00:00.",
            ["createdToUtc"] = "Inclusive creation timestamp upper bound. Must use Z or +00:00.",
            ["sortBy"] = "Sort field: createdAt, shortCode, clickCount, or expiresAt. Defaults to createdAt.",
            ["sortDirection"] = "Sort direction: asc or desc. Defaults to desc. Id is always the stable tie-breaker."
        };

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(ShortUrlsController) ||
            context.MethodInfo.Name != nameof(ShortUrlsController.List))
        {
            return;
        }

        operation.Summary = "List owned short URLs";
        operation.Description =
            "Returns only links owned by the authenticated user. Filtering, ordering, projection, and pagination execute server-side.";

        foreach (var parameter in operation.Parameters?.OfType<OpenApiParameter>() ?? [])
        {
            if (parameter.Name is not null && ParameterDescriptions.TryGetValue(parameter.Name, out var description))
            {
                parameter.Description = description;
                parameter.Name = char.ToLowerInvariant(parameter.Name[0]) + parameter.Name[1..];
            }
        }
    }
}
