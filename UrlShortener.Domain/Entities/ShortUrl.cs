namespace UrlShortener.Domain.Entities;

public class ShortUrl
{
    public Guid Id { get; set; }
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
