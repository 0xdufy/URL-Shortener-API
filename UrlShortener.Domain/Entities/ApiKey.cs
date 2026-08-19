using UrlShortener.Domain.ApiKeys;

namespace UrlShortener.Domain.Entities;

public sealed class ApiKey
{
    private ApiKey()
    {
    }

    public ApiKey(
        Guid id,
        Guid ownerId,
        string name,
        string keyPrefix,
        byte[] secretHash,
        ApiKeyScope scopes,
        DateTime createdAtUtc,
        DateTime? expiresAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("An API key requires a non-empty ID.", nameof(id));
        }

        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("An API key requires a non-empty owner ID.", nameof(ownerId));
        }

        if (string.IsNullOrWhiteSpace(name) || name.Length > 64 || name != name.Trim() ||
            !name.All(IsAllowedNameCharacter) || !char.IsAsciiLetterOrDigit(name[0]))
        {
            throw new ArgumentException("The API-key name is invalid.", nameof(name));
        }

        if (keyPrefix.Length != 26 || !keyPrefix.StartsWith("usk_", StringComparison.Ordinal) ||
            !keyPrefix[4..].All(IsBase64UrlCharacter))
        {
            throw new ArgumentException("The API-key lookup prefix is invalid.", nameof(keyPrefix));
        }

        if (secretHash.Length != 32)
        {
            throw new ArgumentException("An API-key secret hash must contain 32 bytes.", nameof(secretHash));
        }

        const ApiKeyScope supportedScopes =
            ApiKeyScope.ShortUrlsCreate |
            ApiKeyScope.ShortUrlsRead |
            ApiKeyScope.ShortUrlsWrite |
            ApiKeyScope.AnalyticsRead;
        if (scopes == ApiKeyScope.None || (scopes & ~supportedScopes) != ApiKeyScope.None)
        {
            throw new ArgumentOutOfRangeException(nameof(scopes), "At least one supported API-key scope is required.");
        }

        if (createdAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("API-key creation time must be UTC.", nameof(createdAtUtc));
        }

        if (expiresAtUtc.HasValue &&
            (expiresAtUtc.Value.Kind != DateTimeKind.Utc || expiresAtUtc.Value <= createdAtUtc))
        {
            throw new ArgumentException("API-key expiration must be UTC and later than creation.", nameof(expiresAtUtc));
        }

        Id = id;
        OwnerId = ownerId;
        Name = name;
        KeyPrefix = keyPrefix;
        SecretHash = secretHash;
        Scopes = scopes;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    private static bool IsAllowedNameCharacter(char value) =>
        char.IsAsciiLetterOrDigit(value) || value is ' ' or '.' or '_' or '-';

    private static bool IsBase64UrlCharacter(char value) =>
        char.IsAsciiLetterOrDigit(value) || value is '-' or '_';

    public Guid Id { get; private set; }
    public Guid OwnerId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string KeyPrefix { get; private set; } = string.Empty;
    public byte[] SecretHash { get; private set; } = [];
    public ApiKeyScope Scopes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ExpiresAtUtc { get; private set; }
    public DateTime? LastUsedAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public string? RevocationReason { get; private set; }
    public Guid? ReplacedByApiKeyId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public bool IsActiveAt(DateTime utcNow) =>
        !RevokedAtUtc.HasValue && (!ExpiresAtUtc.HasValue || ExpiresAtUtc.Value > utcNow);

    public void Revoke(DateTime revokedAtUtc, string reason, Guid? replacedByApiKeyId = null)
    {
        if (RevokedAtUtc.HasValue)
        {
            throw new InvalidOperationException("The API key is already revoked.");
        }

        RevokedAtUtc = revokedAtUtc;
        RevocationReason = reason;
        ReplacedByApiKeyId = replacedByApiKeyId;
    }

    public void RecordUse(DateTime usedAtUtc)
    {
        if (!LastUsedAtUtc.HasValue || usedAtUtc > LastUsedAtUtc.Value)
        {
            LastUsedAtUtc = usedAtUtc;
        }
    }
}
