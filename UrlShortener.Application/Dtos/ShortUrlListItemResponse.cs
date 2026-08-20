namespace UrlShortener.Application.Dtos;

public sealed class ShortUrlListItemResponse
{
    public Guid Id { get; set; }
    public string OriginalUrl { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public string ShortUrl { get; set; } = string.Empty;
    public Guid? CustomDomainId { get; set; }
    public string? CustomDomainHost { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public bool IsActive { get; set; }
    public bool IsExpired { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public DateTime? RestoreUntilUtc { get; set; }
    public long ClickCount { get; set; }
}
