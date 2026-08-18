namespace UrlShortener.Domain.Entities;

public class ShortUrlAnalyticsDailyVisitor
{
    public Guid ShortUrlId { get; set; }
    public ShortUrl ShortUrl { get; set; } = null!;
    public DateOnly IdentityPeriodUtc { get; set; }
    public string PseudonymousVisitorId { get; set; } = string.Empty;
    public string IdentityScheme { get; set; } = string.Empty;
    public DateTime FirstSeenAtUtc { get; set; }
}
