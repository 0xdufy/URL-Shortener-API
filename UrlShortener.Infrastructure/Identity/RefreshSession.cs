namespace UrlShortener.Infrastructure.Identity;

public sealed class RefreshSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid FamilyId { get; set; }
    public byte[] TokenHash { get; set; } = [];
    public byte[] SecurityStampHash { get; set; } = [];
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime AbsoluteExpiresAtUtc { get; set; }
    public DateTime? LastUsedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? RevocationReason { get; set; }
    public Guid? ReplacedBySessionId { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public ApplicationUser User { get; set; } = null!;
    public RefreshSession? ReplacedBySession { get; set; }
}
