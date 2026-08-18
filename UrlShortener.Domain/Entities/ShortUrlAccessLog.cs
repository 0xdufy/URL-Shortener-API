namespace UrlShortener.Domain.Entities;

public class ShortUrlAccessLog
{
    // Asynchronous analytics uses the stable integration-event ID as this primary key.
    // Legacy synchronous records use a separately generated ID.
    public Guid Id { get; set; }
    public Guid ShortUrlId { get; set; }
    public ShortUrl ShortUrl { get; set; } = null!;
    public DateTime AccessedAtUtc { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Referer { get; set; }
    public string? ReferrerKind { get; set; }
    public string? PseudonymousVisitorId { get; set; }
    public DateOnly? VisitorIdentityPeriodUtc { get; set; }
    public string? VisitorIdentityScheme { get; set; }
}
