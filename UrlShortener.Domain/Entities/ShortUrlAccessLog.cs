namespace UrlShortener.Domain.Entities;

public class ShortUrlAccessLog
{
    public Guid Id { get; set; }
    public Guid ShortUrlId { get; set; }
    public ShortUrl ShortUrl { get; set; } = null!;
    public DateTime AccessedAtUtc { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Referer { get; set; }
}
