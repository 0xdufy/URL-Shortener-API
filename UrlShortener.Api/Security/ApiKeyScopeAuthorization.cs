using Microsoft.AspNetCore.Authorization;
using UrlShortener.Application.ApiKeys;

namespace UrlShortener.Api.Security;

public static class ApiKeyAuthorizationPolicies
{
    public const string ShortUrlsCreate = "ApiKeyScope:shorturls:create";
    public const string ShortUrlsRead = "ApiKeyScope:shorturls:read";
    public const string ShortUrlsWrite = "ApiKeyScope:shorturls:write";
    public const string AnalyticsRead = "ApiKeyScope:analytics:read";
}

public sealed record ApiKeyScopeRequirement(string Scope) : IAuthorizationRequirement;

public sealed class ApiKeyScopeAuthorizationHandler : AuthorizationHandler<ApiKeyScopeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ApiKeyScopeRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        var isApiKey = context.User.HasClaim(
            claim => claim.Type == ApiKeyAuthenticationDefaults.ApiKeyIdClaim);
        if (!isApiKey || context.User.HasClaim(ApiKeyAuthenticationDefaults.ScopeClaim, requirement.Scope))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
