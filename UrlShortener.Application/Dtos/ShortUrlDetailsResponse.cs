namespace UrlShortener.Application.Dtos;

public class ShortUrlDetailsResponse
{
    public Guid Id { get; set; }
    public string OriginalUrl { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public long ClickCount { get; set; }
    public DateTime? LastAccessedAtUtc { get; set; }
}
