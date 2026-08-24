namespace UrlShortener.Domain.Entities;

public sealed class ShortUrlModerationAction
{
    private ShortUrlModerationAction()
    {
    }

    public ShortUrlModerationAction(
        Guid shortUrlId,
        Guid actorUserId,
        string action,
        string? publicReasonCode,
        string internalReason,
        DateTime occurredAtUtc)
    {
        Id = Guid.NewGuid();
        ShortUrlId = shortUrlId;
        ActorUserId = actorUserId;
        Action = action;
        PublicReasonCode = publicReasonCode;
        InternalReason = internalReason;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid ShortUrlId { get; private set; }
    public Guid ActorUserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string? PublicReasonCode { get; private set; }
    public string InternalReason { get; private set; } = string.Empty;
    public DateTime OccurredAtUtc { get; private set; }
}
