namespace UrlShortener.Analytics.Worker.Maintenance;

public interface IMaintenanceJob
{
    string Name { get; }

    Task ExecuteAsync(CancellationToken cancellationToken);
}
