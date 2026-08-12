using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using UrlShortener.Api.Middlewares;
using UrlShortener.Api.Security;
using UrlShortener.Application.Interfaces;
using UrlShortener.Application.RateLimiting;

namespace UrlShortener.Api.RateLimiting;

public sealed class DistributedRateLimitingMiddleware
{
    public const string ApiKeyIdClaim = "api_key_id";

    private readonly RequestDelegate _next;

    public DistributedRateLimitingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IDistributedRateLimiter rateLimiter)
    {
        var metadata = context.GetEndpoint()?
            .Metadata
            .GetOrderedMetadata<DistributedRateLimitAttribute>();
        var selectedPolicy = metadata?.LastOrDefault()?.Policy;
        if (!selectedPolicy.HasValue)
        {
            await _next(context);
            return;
        }

        var partitionKey = ResolvePartitionKey(context, selectedPolicy.Value);
        if (partitionKey is null)
        {
            // Authentication/authorization owns missing or invalid credentials.
            await _next(context);
            return;
        }

        var decision = await rateLimiter.CheckAsync(
            selectedPolicy.Value,
            partitionKey,
            context.RequestAborted);
        if (!decision.IsAllowed)
        {
            throw new RateLimitedException(
                $"Rate limit exceeded. Retry after {decision.RetryAfterSeconds} seconds.",
                decision.RetryAfterSeconds);
        }

        await _next(context);
    }

    private static string? ResolvePartitionKey(HttpContext context, RateLimitPolicy policy) => policy switch
    {
        RateLimitPolicy.Anonymous or
        RateLimitPolicy.AuthenticationRegistration or
        RateLimitPolicy.AuthenticationSignIn or
        RateLimitPolicy.AuthenticationSession => $"ip:{GetDirectClientIp(context)}",
        RateLimitPolicy.Authenticated or
        RateLimitPolicy.UrlCreation => GetAuthenticatedUserPartition(context.User),
        RateLimitPolicy.ApiKey => GetApiKeyPartition(context.User),
        _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, null)
    };

    private static string GetDirectClientIp(HttpContext context)
    {
        var address = context.Connection.RemoteIpAddress;
        if (address is null)
        {
            return "unknown";
        }

        return address.IsIPv4MappedToIPv6
            ? address.MapToIPv4().ToString()
            : address.ToString();
    }

    private static string? GetAuthenticatedUserPartition(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var subject = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(subject, out var userId) && userId != Guid.Empty
            ? $"user:{userId:D}"
            : null;
    }

    private static string? GetApiKeyPartition(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var apiKeyId = principal.FindFirstValue(ApiKeyIdClaim);
        return string.IsNullOrWhiteSpace(apiKeyId)
            ? null
            : $"api-key:{apiKeyId}";
    }
}
