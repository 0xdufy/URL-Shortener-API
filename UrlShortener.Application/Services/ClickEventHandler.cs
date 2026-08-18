using UrlShortener.Application.Interfaces;
using UrlShortener.Application.Messaging;

namespace UrlShortener.Application.Services;

public sealed class ClickEventHandler
{
    private const int MaximumVisitorIdentifierLength = 64;
    private const int MaximumReferrerHostLength = 253;
    private const int MaximumUserAgentLength = 256;

    private readonly IClickEventPersistence _persistence;

    public ClickEventHandler(IClickEventPersistence persistence)
    {
        _persistence = persistence;
    }

    public async Task<EventHandlingOutcome> HandleAsync(
        IntegrationEvent<ClickEventV1> integrationEvent,
        CancellationToken cancellationToken = default)
    {
        if (!IsValid(integrationEvent))
        {
            return EventHandlingOutcome.DeadLetter;
        }

        var outcome = await _persistence.PersistAsync(integrationEvent, cancellationToken);
        return outcome switch
        {
            ClickEventPersistenceOutcome.Persisted => EventHandlingOutcome.Completed,
            ClickEventPersistenceOutcome.Duplicate => EventHandlingOutcome.Completed,
            ClickEventPersistenceOutcome.ShortUrlNotFound => EventHandlingOutcome.DeadLetter,
            _ => throw new InvalidOperationException($"Unsupported persistence outcome '{outcome}'.")
        };
    }

    private static bool IsValid(IntegrationEvent<ClickEventV1> integrationEvent)
    {
        var payload = integrationEvent.Payload;
        return integrationEvent.EventId != Guid.Empty &&
            string.Equals(integrationEvent.EventName, ClickEventContract.EventName, StringComparison.Ordinal) &&
            integrationEvent.ContractVersion == ClickEventContract.Version &&
            integrationEvent.OccurredAtUtc.Offset == TimeSpan.Zero &&
            payload is not null &&
            payload.ShortUrlId != Guid.Empty &&
            payload.AccessedAtUtc.Offset == TimeSpan.Zero &&
            payload.AccessedAtUtc == integrationEvent.OccurredAtUtc &&
            payload.VisitorIdentityPeriodUtc == DateOnly.FromDateTime(payload.AccessedAtUtc.UtcDateTime) &&
            string.Equals(
                payload.VisitorIdentityScheme,
                ClickEventContract.VisitorIdentityScheme,
                StringComparison.Ordinal) &&
            !string.IsNullOrWhiteSpace(payload.PseudonymousVisitorId) &&
            payload.PseudonymousVisitorId.Length <= MaximumVisitorIdentifierLength &&
            IsValidReferrer(payload.ReferrerHost, payload.ReferrerKind) &&
            (payload.UserAgent is null || payload.UserAgent.Length <= MaximumUserAgentLength);
    }

    private static bool IsValidReferrer(string? referrerHost, string? referrerKind)
    {
        if (referrerKind is null)
        {
            return referrerHost is null || referrerHost.Length <= MaximumReferrerHostLength;
        }

        return referrerKind switch
        {
            ClickEventContract.ReferrerKindDirect or ClickEventContract.ReferrerKindUnknown =>
                referrerHost is null,
            ClickEventContract.ReferrerKindHost =>
                !string.IsNullOrWhiteSpace(referrerHost) &&
                referrerHost.Length <= MaximumReferrerHostLength,
            _ => false
        };
    }
}
