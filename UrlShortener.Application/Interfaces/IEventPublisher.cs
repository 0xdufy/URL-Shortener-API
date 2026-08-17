using UrlShortener.Application.Messaging;

namespace UrlShortener.Application.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync<TPayload>(
        IntegrationEvent<TPayload> integrationEvent,
        CancellationToken cancellationToken = default);
}
