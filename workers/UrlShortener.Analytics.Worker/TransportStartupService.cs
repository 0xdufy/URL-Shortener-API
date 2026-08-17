using UrlShortener.Infrastructure.Messaging;

namespace UrlShortener.Analytics.Worker;

public sealed class TransportStartupService : IHostedService
{
    private readonly RabbitMqTopologyInitializer _topologyInitializer;
    private readonly ILogger<TransportStartupService> _logger;

    public TransportStartupService(
        RabbitMqTopologyInitializer topologyInitializer,
        ILogger<TransportStartupService> logger)
    {
        _topologyInitializer = topologyInitializer;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _topologyInitializer.EnsureTopologyAsync(cancellationToken);
        _logger.LogInformation(
            "RabbitMQ click-event topology is available. Analytics consumption is introduced by TASK-031.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
