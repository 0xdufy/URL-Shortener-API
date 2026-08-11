using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using UrlShortener.Api.Models;

namespace UrlShortener.Api.OpenApi;

public sealed class AuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var requiresAuthorization = context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<IAuthorizeData>()
            .Any();
        if (!requiresAuthorization)
        {
            return;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
        });

        operation.Responses ??= [];
        AddErrorResponse(operation, context, "401", "Authentication is required.");
        AddErrorResponse(operation, context, "403", "The authenticated identity is forbidden by a non-resource policy.");
    }

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
