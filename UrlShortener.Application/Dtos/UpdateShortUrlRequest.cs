namespace UrlShortener.Application.Dtos;

public sealed class UpdateShortUrlRequest
{
    public string OriginalUrl { get; set; } = string.Empty;
    public Guid? CustomDomainId { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
}
