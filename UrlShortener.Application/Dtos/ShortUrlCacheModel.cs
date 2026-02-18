namespace UrlShortener.Application.Dtos;

public class ShortUrlCacheModel
{
    public string OriginalUrl { get; set; } = string.Empty;
    public DateTime? ExpiresAtUtc { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
}
