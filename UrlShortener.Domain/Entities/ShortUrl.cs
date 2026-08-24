using UrlShortener.Domain.Moderation;

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
    public Guid? CustomDomainId { get; private set; }
    public CustomDomain? CustomDomain { get; private set; }
    public string OriginalUrl { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public long ClickCount { get; set; }
    public DateTime? LastAccessedAtUtc { get; set; }
    public ShortUrlModerationStatus ModerationStatus { get; private set; } = ShortUrlModerationStatus.Unreviewed;
    public string? ModerationPublicReasonCode { get; private set; }
    public DateTime? ModeratedAtUtc { get; private set; }
    public Guid? ModeratedByUserId { get; private set; }
    public ICollection<ShortUrlAccessLog> AccessLogs { get; set; } = new List<ShortUrlAccessLog>();

    public void ApplyModeration(
        ShortUrlModerationStatus status,
        string? publicReasonCode,
        Guid actorUserId,
        DateTime moderatedAtUtc)
    {
        if (status == ShortUrlModerationStatus.Unreviewed)
        {
            throw new ArgumentException("A moderation action must clear or block a short URL.", nameof(status));
        }

        ModerationStatus = status;
        ModerationPublicReasonCode = status == ShortUrlModerationStatus.Blocked
            ? publicReasonCode
            : null;
        ModeratedByUserId = actorUserId;
        ModeratedAtUtc = moderatedAtUtc;
    }

    public void RouteThrough(CustomDomain? customDomain)
    {
        if (customDomain != null &&
            (!OwnerId.HasValue || customDomain.OwnerId != OwnerId.Value))
        {
            throw new ArgumentException(
                "A short URL can only use a custom domain owned by the same account.",
                nameof(customDomain));
        }

        CustomDomainId = customDomain?.Id;
        CustomDomain = customDomain;
    }
}
