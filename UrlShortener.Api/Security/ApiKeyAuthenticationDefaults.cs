namespace UrlShortener.Api.Security;

public static class ApiKeyAuthenticationDefaults
{
    public const string CompositeScheme = "BearerOrApiKey";
    public const string AuthenticationScheme = "ApiKey";
    public const string AuthorizationHeaderPrefix = "ApiKey ";
    public const string ApiKeyIdClaim = "api_key_id";
    public const string ScopeClaim = "scope";
}
