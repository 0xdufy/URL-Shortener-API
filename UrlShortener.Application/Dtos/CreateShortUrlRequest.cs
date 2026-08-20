namespace UrlShortener.Application.Dtos;

public class CreateShortUrlRequest
{
    public string OriginalUrl { get; set; } = string.Empty;
    public string? CustomAlias { get; set; }
    public Guid? CustomDomainId { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
}
