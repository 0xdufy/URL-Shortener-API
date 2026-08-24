namespace UrlShortener.Analytics.Worker.Maintenance;

public sealed class MaintenanceSchedulingOptions
{
    public const string SectionName = "MaintenanceJobs";

    public bool Enabled { get; set; } = true;

    public int PollIntervalSeconds { get; set; } = 5;

    public Dictionary<string, MaintenanceJobScheduleOptions> Jobs { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}

public sealed class MaintenanceJobScheduleOptions
{
    public bool Enabled { get; set; }

    public int IntervalSeconds { get; set; } = 300;

    public int TimeoutSeconds { get; set; } = 30;

    public int MaxAttempts { get; set; } = 3;

    public int RetryDelaySeconds { get; set; } = 5;
}
