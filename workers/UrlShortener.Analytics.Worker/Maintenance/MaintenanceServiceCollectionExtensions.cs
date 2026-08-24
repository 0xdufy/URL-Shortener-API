using UrlShortener.Application.BackgroundJobs;
using UrlShortener.Infrastructure.BackgroundJobs;

namespace UrlShortener.Analytics.Worker.Maintenance;

public static class MaintenanceServiceCollectionExtensions
{
    public static IServiceCollection AddMaintenanceScheduling(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<MaintenanceSchedulingOptions>()
            .Bind(configuration.GetRequiredSection(MaintenanceSchedulingOptions.SectionName))
            .Validate(
                options => options.PollIntervalSeconds is >= 1 and <= 300,
                "MaintenanceJobs:PollIntervalSeconds must be between 1 and 300.")
            .Validate(
                options => options.Jobs.Count > 0,
                "MaintenanceJobs:Jobs must contain at least one registered job schedule.")
            .Validate(
                options => options.Jobs.Keys.All(name =>
                    !string.IsNullOrWhiteSpace(name) && name.Length <= 200),
                "Maintenance job names must be non-empty and no longer than 200 characters.")
            .Validate(
                options => options.Jobs.Values.All(schedule =>
                    schedule.IntervalSeconds is >= 1 and <= 86400 &&
                    schedule.TimeoutSeconds is >= 1 and <= 3600 &&
                    schedule.MaxAttempts is >= 1 and <= 10 &&
                    schedule.RetryDelaySeconds is >= 0 and <= 300),
                "Every maintenance job requires IntervalSeconds 1-86400, TimeoutSeconds 1-3600, MaxAttempts 1-10, and RetryDelaySeconds 0-300.")
            .ValidateOnStart();

        services.AddSingleton<IDistributedJobLock, SqlServerDistributedJobLock>();
        services.AddSingleton<IMaintenanceJob, FoundationHeartbeatJob>();
        services.AddHostedService<MaintenanceSchedulerService>();
        return services;
    }
}
