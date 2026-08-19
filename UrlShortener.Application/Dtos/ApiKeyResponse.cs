namespace UrlShortener.Application.Dtos;

public sealed class ApiKeyResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Prefix { get; init; } = string.Empty;
    public IReadOnlyList<string> Scopes { get; init; } = [];
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? ExpiresAtUtc { get; init; }
    public DateTime? LastUsedAtUtc { get; init; }
    public DateTime? RevokedAtUtc { get; init; }
    public string State { get; init; } = string.Empty;
    public Guid? ReplacedByApiKeyId { get; init; }
}
