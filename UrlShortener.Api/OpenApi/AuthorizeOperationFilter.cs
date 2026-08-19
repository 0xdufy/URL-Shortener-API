using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using UrlShortener.Api.Models;
using UrlShortener.Api.Security;
using UrlShortener.Application.ApiKeys;

namespace UrlShortener.Api.OpenApi;

public sealed class AuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var authorization = context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<IAuthorizeData>()
            .ToArray();
        if (authorization.Length == 0)
        {
            return;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
        });

        var requiredScope = authorization
            .Select(data => GetApiKeyScope(data.Policy))
            .FirstOrDefault(scope => scope != null);
        if (requiredScope != null)
        {
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("ApiKey", context.Document)] = []
            });

            var scopeDescription = $"API-key callers require the `{requiredScope}` scope.";
            operation.Description = string.IsNullOrWhiteSpace(operation.Description)
                ? scopeDescription
                : $"{operation.Description}\n\n{scopeDescription}";
        }

        operation.Responses ??= [];
        AddErrorResponse(operation, context, "401", "Authentication is required.");
        AddErrorResponse(operation, context, "403", "The authenticated identity is forbidden by a non-resource policy.");
    }

    private static string? GetApiKeyScope(string? policy) => policy switch
    {
        ApiKeyAuthorizationPolicies.ShortUrlsCreate => ApiKeyScopeNames.ShortUrlsCreate,
        ApiKeyAuthorizationPolicies.ShortUrlsRead => ApiKeyScopeNames.ShortUrlsRead,
        ApiKeyAuthorizationPolicies.ShortUrlsWrite => ApiKeyScopeNames.ShortUrlsWrite,
        ApiKeyAuthorizationPolicies.AnalyticsRead => ApiKeyScopeNames.AnalyticsRead,
        _ => null
    };

    private static void AddErrorResponse(
        OpenApiOperation operation,
        OperationFilterContext context,
        string statusCode,
        string description)
    {
        if (operation.Responses!.ContainsKey(statusCode))
        {
            return;
        }

        var schema = context.SchemaGenerator.GenerateSchema(typeof(ErrorResponse), context.SchemaRepository);
        operation.Responses[statusCode] = new OpenApiResponse
        {
            Description = description,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new() { Schema = schema }
            }
        };
    }
}
