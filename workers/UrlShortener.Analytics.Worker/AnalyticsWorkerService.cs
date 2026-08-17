using UrlShortener.Application.Interfaces;
using UrlShortener.Application.Messaging;
using UrlShortener.Application.Services;

namespace UrlShortener.Analytics.Worker;

public sealed class AnalyticsWorkerService : BackgroundService
{
    private readonly IEventConsumer _consumer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AnalyticsWorkerService> _logger;

    public AnalyticsWorkerService(
        IEventConsumer consumer,
        IServiceScopeFactory scopeFactory,
        ILogger<AnalyticsWorkerService> logger)
    {
        _consumer = consumer;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Analytics worker is consuming {EventName} contract version {ContractVersion}.",
            ClickEventContract.EventName,
            ClickEventContract.Version);

        await _consumer.ConsumeAsync<ClickEventV1>(
            ClickEventContract.EventName,
            ClickEventContract.Version,
            HandleAsync,
            stoppingToken);
    }

    private async Task<EventHandlingOutcome> HandleAsync(
        IntegrationEvent<ClickEventV1> integrationEvent,
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ClickEventHandler>();
        var outcome = await handler.HandleAsync(integrationEvent, cancellationToken);

        if (outcome == EventHandlingOutcome.DeadLetter)
        {
            _logger.LogWarning(
                "Click event {EventId} for short URL {ShortUrlId} is permanently invalid or references a missing link and will be dead-lettered.",
                integrationEvent.EventId,
                integrationEvent.Payload?.ShortUrlId);
        }
        else
        {
            _logger.LogDebug(
                "Click event {EventId} for short URL {ShortUrlId} is durably persisted or was already processed.",
                integrationEvent.EventId,
                integrationEvent.Payload.ShortUrlId);
        }

        return outcome;
    }
}
