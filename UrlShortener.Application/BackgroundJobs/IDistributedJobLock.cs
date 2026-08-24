namespace UrlShortener.Application.BackgroundJobs;

public interface IDistributedJobLock
{
    Task<IDistributedJobLease?> TryAcquireAsync(
        string resourceName,
        CancellationToken cancellationToken);
}

public interface IDistributedJobLease : IAsyncDisposable;
