using UrlShortener.Application.Dtos;

namespace UrlShortener.Application.Interfaces;

public interface IRedirectClickEventPublisher
{
    Task PublishBestEffortAsync(
        RedirectClickEventRequest request,
        CancellationToken cancellationToken = default);
}
