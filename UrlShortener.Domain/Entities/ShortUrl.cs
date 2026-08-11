namespace UrlShortener.Domain.Entities;

public class ShortUrl
{
    private ShortUrl()
    {
    }

    public ShortUrl(Guid ownerId)
    {
        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("An owned short URL requires a non-empty owner ID.", nameof(ownerId));
        }

        OwnerId = ownerId;
    }

    public Guid Id { get; set; }
    public Guid? OwnerId { get; private set; }
    public string OriginalUrl { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public long ClickCount { get; set; }
    public DateTime? LastAccessedAtUtc { get; set; }
    public ICollection<ShortUrlAccessLog> AccessLogs { get; set; } = new List<ShortUrlAccessLog>();
}
