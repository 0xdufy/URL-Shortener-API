namespace UrlShortener.Api.Middlewares;

public sealed class SecurityHeadersMiddleware
{
    private const string AuthenticationPath = "/api/v1/auth";
    private const string SwaggerPath = "/swagger";

    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(static state =>
        {
            var httpContext = (HttpContext)state;
            var headers = httpContext.Response.Headers;

            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Permissions-Policy"] = "camera=(), geolocation=(), microphone=()";
            headers["X-Permitted-Cross-Domain-Policies"] = "none";

            if (!httpContext.Request.Path.StartsWithSegments(SwaggerPath))
            {
                headers["Content-Security-Policy"] =
                    "default-src 'none'; base-uri 'none'; form-action 'none'; frame-ancestors 'none'";
            }

            if (httpContext.Request.Path.StartsWithSegments(AuthenticationPath))
            {
                headers.CacheControl = "no-store";
                headers.Pragma = "no-cache";
            }

            return Task.CompletedTask;
        }, context);

        await _next(context);
    }
}
