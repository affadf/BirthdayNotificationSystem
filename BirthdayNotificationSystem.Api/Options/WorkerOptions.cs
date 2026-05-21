namespace BirthdayNotificationSystem.Api.Options;

/// <summary>
/// Configuration values controlling the notification background worker.
/// </summary>
public sealed class WorkerOptions
{
    /// <summary>
    /// Configuration section name used by the options binder.
    /// </summary>
    public const string SectionName = "NotificationWorker";

    /// <summary>
    /// Enables or disables the hosted notification worker.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum number of due notifications to inspect per polling cycle.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Delay in seconds between worker polling cycles.
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Number of minutes a notification claim remains valid before recovery can retry it.
    /// </summary>
    public int LockTimeoutMinutes { get; set; } = 5;

}
