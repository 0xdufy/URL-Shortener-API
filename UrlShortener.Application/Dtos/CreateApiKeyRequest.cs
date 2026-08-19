namespace UrlShortener.Application.Dtos;

public sealed class CreateApiKeyRequest
{
    public string Name { get; set; } = string.Empty;
    public IReadOnlyList<string> Scopes { get; set; } = [];
    public DateTime? ExpiresAtUtc { get; set; }
}
