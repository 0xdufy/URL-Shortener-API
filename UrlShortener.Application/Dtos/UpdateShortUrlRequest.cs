namespace UrlShortener.Application.Dtos;

public sealed class UpdateShortUrlRequest
{
    public string OriginalUrl { get; set; } = string.Empty;
    public DateTime? ExpiresAtUtc { get; set; }
}
