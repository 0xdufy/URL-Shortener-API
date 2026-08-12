using UrlShortener.Application.Dtos;

namespace UrlShortener.Application.Interfaces;

public interface IRedirectAccessRecorder
{
    Task<bool> TryRecordAsync(RedirectAccessRequest request, CancellationToken ct);
}
