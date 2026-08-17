namespace UrlShortener.Application.Messaging;

public sealed record IntegrationEvent<TPayload>(
    Guid EventId,
    string EventName,
    int ContractVersion,
    DateTimeOffset OccurredAtUtc,
    TPayload Payload);
