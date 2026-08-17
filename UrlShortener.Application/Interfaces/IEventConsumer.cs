using UrlShortener.Application.Messaging;

namespace UrlShortener.Application.Interfaces;

public interface IEventConsumer
{
    Task ConsumeAsync<TPayload>(
        string expectedEventName,
        int expectedContractVersion,
        Func<IntegrationEvent<TPayload>, CancellationToken, Task<EventHandlingOutcome>> handler,
        CancellationToken cancellationToken = default);
}
