namespace UrlShortener.Domain.Entities;

public sealed class ShortUrlCreationIdempotencyRecord
{
    private ShortUrlCreationIdempotencyRecord()
    {
    }

    public ShortUrlCreationIdempotencyRecord(
        Guid ownerId,
        string keyHash,
        string requestHash,
        Guid shortUrlId,
        DateTime createdAtUtc,
        DateTime expiresAtUtc)
    {
        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("An idempotency record requires a non-empty owner ID.", nameof(ownerId));
        }

        OwnerId = ownerId;
        KeyHash = keyHash;
        RequestHash = requestHash;
        ShortUrlId = shortUrlId;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid Id { get; set; }
    public Guid OwnerId { get; private set; }
    public string KeyHash { get; private set; } = string.Empty;
    public string RequestHash { get; private set; } = string.Empty;
    public Guid ShortUrlId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public ShortUrl ShortUrl { get; private set; } = null!;
}
