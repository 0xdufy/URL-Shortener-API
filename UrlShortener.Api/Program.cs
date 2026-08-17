using UrlShortener.Api.Extensions;
using UrlShortener.Api.Configuration;
using UrlShortener.Api.Middlewares;
using UrlShortener.Api.OpenApi;
using UrlShortener.Api.Models;
using UrlShortener.Api.RateLimiting;
using Microsoft.OpenApi;
using Microsoft.Extensions.Options;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogLogging();
builder.Services.AddApiServices(builder.Configuration, builder.Environment);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Description = "Enter the JWT access token returned by the authentication endpoints."
    });
    options.OperationFilter<AuthorizeOperationFilter>();
    options.OperationFilter<OwnedShortUrlListOperationFilter>();
    options.OperationFilter<ShortUrlLifecycleOperationFilter>();
});

var app = builder.Build();

var proxyTrust = app.Services.GetRequiredService<IOptions<ProxyTrustOptions>>().Value;
if (proxyTrust.Enabled)
{
    app.Logger.LogInformation(
        "Forwarded client IP processing is enabled with {KnownProxyCount} known proxies, {KnownNetworkCount} known networks, and a {ForwardLimit}-hop limit.",
        proxyTrust.KnownProxies.Length,
        proxyTrust.KnownNetworks.Length,
        proxyTrust.ForwardLimit);
}
else if (proxyTrust.KnownProxies.Length + proxyTrust.KnownNetworks.Length > 0)
{
    app.Logger.LogWarning(
        "Forwarded client IP processing is disabled; {TrustEntryCount} configured trust entries will be ignored.",
        proxyTrust.KnownProxies.Length + proxyTrust.KnownNetworks.Length);
}
else
{
    app.Logger.LogInformation("Forwarded client IP processing is disabled; forwarding headers will be ignored.");
}

app.UseForwardedHeaders();
app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseStatusCodePages(async statusCodeContext =>
{
    var response = statusCodeContext.HttpContext.Response;
    var (code, message) = response.StatusCode switch
    {
        StatusCodes.Status400BadRequest => ("BAD_REQUEST", "The request is invalid."),
        StatusCodes.Status404NotFound => ("NOT_FOUND", "Resource not found."),
        StatusCodes.Status405MethodNotAllowed => ("METHOD_NOT_ALLOWED", "The HTTP method is not allowed for this resource."),
        StatusCodes.Status415UnsupportedMediaType => ("UNSUPPORTED_MEDIA_TYPE", "The request media type is not supported."),
        _ => ("HTTP_ERROR", "The request could not be completed.")
    };

    response.ContentType = "application/json";
    await response.WriteAsJsonAsync(ApiErrorFactory.Create(statusCodeContext.HttpContext, code, message));
});
app.UseCors("TrustedWebClient");
app.UseAuthentication();
app.UseMiddleware<DistributedRateLimitingMiddleware>();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();
