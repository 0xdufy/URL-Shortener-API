namespace UrlShortener.Application.Dtos;

public sealed class ApiKeyCreationResponse
{
    public ApiKeyResponse ApiKey { get; init; } = new();
    public string Key { get; init; } = string.Empty;
}
