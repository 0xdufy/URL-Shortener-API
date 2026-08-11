using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using UrlShortener.Api.Controllers;

namespace UrlShortener.Api.OpenApi;

public sealed class ShortUrlLifecycleOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(ShortUrlsController))
        {
            return;
        }

        switch (context.MethodInfo.Name)
        {
            case nameof(ShortUrlsController.Update):
                operation.Summary = "Replace mutable short URL fields";
                operation.Description =
                    "Replaces the destination URL and expiry for a non-deleted link owned by the authenticated user. " +
                    "A null or omitted expiresAtUtc clears expiry. The short code, owner, identity, creation time, and counters are immutable.";
                break;
            case nameof(ShortUrlsController.UpdateStatus):
                operation.Summary = "Activate or deactivate an owned short URL";
                operation.Description =
                    "Requires an explicit Boolean isActive value and sets the active state of a non-deleted owned link. " +
                    "Inactive links return 404 from the public redirect route.";
                break;
            case nameof(ShortUrlsController.Delete):
                operation.Summary = "Soft-delete an owned short URL";
                operation.Description =
                    "Marks an owned link deleted, records the deletion time, and makes the public redirect return 404. " +
                    "The short-code claim and access history are retained.";
                break;
            case nameof(ShortUrlsController.Restore):
                operation.Summary = "Restore a soft-deleted owned short URL";
                operation.Description =
                    "Restores a deleted owned link before its configured retention boundary. The pre-delete active state is preserved; " +
                    "an expired destination remains expired after restore.";
                break;
        }
    }
}
