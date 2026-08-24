namespace UrlShortener.Analytics.Worker.Maintenance;

public sealed class FoundationHeartbeatJob : IMaintenanceJob
{
    public const string JobName = "foundation-heartbeat";

    private readonly ILogger<FoundationHeartbeatJob> _logger;

    public FoundationHeartbeatJob(ILogger<FoundationHeartbeatJob> logger)
    {
        _logger = logger;
    }

    public string Name => JobName;

    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation(
            "Maintenance scheduler heartbeat observed at {ObservedAtUtc}.",
            DateTimeOffset.UtcNow);
        return Task.CompletedTask;
    }
}
