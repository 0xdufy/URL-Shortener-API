using UrlShortener.Application.Dtos;

namespace UrlShortener.Application.Interfaces;

public interface IRedirectResolver
{
    Task<RedirectResolutionResult> ResolveAsync(
        string effectiveHost,
        string shortCode,
        string ipAddress,
        string? userAgent,
        string? referer,
        CancellationToken ct);
}
