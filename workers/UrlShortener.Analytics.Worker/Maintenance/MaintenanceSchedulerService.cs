using Microsoft.Extensions.Options;
using UrlShortener.Application.BackgroundJobs;

namespace UrlShortener.Analytics.Worker.Maintenance;

public sealed class MaintenanceSchedulerService : BackgroundService
{
    private readonly IReadOnlyDictionary<string, IMaintenanceJob> _jobs;
    private readonly MaintenanceSchedulingOptions _options;
    private readonly IDistributedJobLock _distributedLock;
    private readonly ILogger<MaintenanceSchedulerService> _logger;
    private readonly Dictionary<string, DateTimeOffset> _nextRuns =
        new(StringComparer.OrdinalIgnoreCase);

    public MaintenanceSchedulerService(
        IEnumerable<IMaintenanceJob> jobs,
        IOptions<MaintenanceSchedulingOptions> options,
        IDistributedJobLock distributedLock,
        ILogger<MaintenanceSchedulerService> logger)
    {
        _jobs = jobs.ToDictionary(job => job.Name, StringComparer.OrdinalIgnoreCase);
        _options = options.Value;
        _distributedLock = distributedLock;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Maintenance scheduling is disabled by configuration.");
            return;
        }

        ValidateRegistrations();

        var enabledJobs = _options.Jobs
            .Where(pair => pair.Value.Enabled)
            .Select(pair => (_jobs[pair.Key], pair.Value))
            .ToArray();

        if (enabledJobs.Length == 0)
        {
            _logger.LogInformation("Maintenance scheduling is enabled with no enabled jobs.");
            return;
        }

        _logger.LogInformation(
            "Maintenance scheduler started with {EnabledJobCount} enabled job(s) and a {PollIntervalSeconds}-second polling interval.",
            enabledJobs.Length,
            _options.PollIntervalSeconds);

        var startedAt = DateTimeOffset.UtcNow;
        foreach (var (job, _) in enabledJobs)
        {
            _nextRuns[job.Name] = startedAt;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.PollIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunDueJobsAsync(enabledJobs, stoppingToken);

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                {
                    break;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task RunDueJobsAsync(
        IEnumerable<(IMaintenanceJob Job, MaintenanceJobScheduleOptions Schedule)> jobs,
        CancellationToken stoppingToken)
    {
        var now = DateTimeOffset.UtcNow;
        var dueJobs = jobs
            .Where(item => _nextRuns[item.Job.Name] <= now)
            .ToArray();

        foreach (var (job, schedule) in dueJobs)
        {
            _nextRuns[job.Name] = now.AddSeconds(schedule.IntervalSeconds);
        }

        await Task.WhenAll(dueJobs.Select(item =>
            RunJobAsync(item.Job, item.Schedule, stoppingToken)));
    }

    private async Task RunJobAsync(
        IMaintenanceJob job,
        MaintenanceJobScheduleOptions schedule,
        CancellationToken stoppingToken)
    {
        var runId = Guid.NewGuid();
        IDistributedJobLease? lease;

        try
        {
            lease = await _distributedLock.TryAcquireAsync(job.Name, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Maintenance job {JobName} run {RunId} could not acquire its distributed lock.",
                job.Name,
                runId);
            return;
        }

        if (lease is null)
        {
            _logger.LogInformation(
                "Maintenance job {JobName} run {RunId} was skipped because another worker instance owns the lock.",
                job.Name,
                runId);
            return;
        }

        await using (lease)
        {
            var startedAt = DateTimeOffset.UtcNow;
            _logger.LogInformation(
                "Maintenance job {JobName} run {RunId} started with timeout {TimeoutSeconds}s and at most {MaxAttempts} attempt(s).",
                job.Name,
                runId,
                schedule.TimeoutSeconds,
                schedule.MaxAttempts);

            Exception? lastFailure = null;

            for (var attempt = 1; attempt <= schedule.MaxAttempts; attempt++)
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(schedule.TimeoutSeconds));

                try
                {
                    await job.ExecuteAsync(timeout.Token);
                    _logger.LogInformation(
                        "Maintenance job {JobName} run {RunId} succeeded on attempt {Attempt} in {DurationMilliseconds}ms.",
                        job.Name,
                        runId,
                        attempt,
                        (DateTimeOffset.UtcNow - startedAt).TotalMilliseconds);
                    return;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation(
                        "Maintenance job {JobName} run {RunId} was cancelled during worker shutdown.",
                        job.Name,
                        runId);
                    return;
                }
                catch (OperationCanceledException exception) when (timeout.IsCancellationRequested)
                {
                    lastFailure = exception;
                    _logger.LogWarning(
                        "Maintenance job {JobName} run {RunId} attempt {Attempt} timed out after {TimeoutSeconds}s.",
                        job.Name,
                        runId,
                        attempt,
                        schedule.TimeoutSeconds);
                }
                catch (Exception exception)
                {
                    lastFailure = exception;
                    _logger.LogWarning(
                        exception,
                        "Maintenance job {JobName} run {RunId} attempt {Attempt} failed.",
                        job.Name,
                        runId,
                        attempt);
                }

                if (attempt < schedule.MaxAttempts)
                {
                    try
                    {
                        await Task.Delay(
                            TimeSpan.FromSeconds(schedule.RetryDelaySeconds),
                            stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        return;
                    }
                }
            }

            _logger.LogError(
                lastFailure,
                "Maintenance job {JobName} run {RunId} failed after {MaxAttempts} attempt(s) in {DurationMilliseconds}ms.",
                job.Name,
                runId,
                schedule.MaxAttempts,
                (DateTimeOffset.UtcNow - startedAt).TotalMilliseconds);
        }
    }

    private void ValidateRegistrations()
    {
        var unknownJobs = _options.Jobs.Keys
            .Where(name => !_jobs.ContainsKey(name))
            .ToArray();

        if (unknownJobs.Length > 0)
        {
            throw new InvalidOperationException(
                $"MaintenanceJobs:Jobs contains unregistered jobs: {string.Join(", ", unknownJobs)}.");
        }
    }
}
