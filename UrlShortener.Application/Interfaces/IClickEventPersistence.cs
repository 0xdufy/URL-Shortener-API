using UrlShortener.Application.Messaging;

namespace UrlShortener.Application.Interfaces;

public interface IClickEventPersistence
{
    Task<ClickEventPersistenceOutcome> PersistAsync(
        IntegrationEvent<ClickEventV1> integrationEvent,
        CancellationToken cancellationToken = default);
}

public enum ClickEventPersistenceOutcome
{
    Persisted,
    Duplicate,
    ShortUrlNotFound
}
