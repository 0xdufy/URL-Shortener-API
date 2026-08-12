using UrlShortener.Application.Dtos;

namespace UrlShortener.Application.Interfaces;

public interface IRedirectResolver
{
    Task<RedirectResolutionResult> ResolveAsync(
        string shortCode,
        string ipAddress,
        string? userAgent,
        string? referer,
        CancellationToken ct);
}
