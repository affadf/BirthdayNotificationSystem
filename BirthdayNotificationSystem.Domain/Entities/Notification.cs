namespace BirthdayNotificationSystem.Domain;

/// <summary>
/// Represents a durable notification record processed by the background worker.
/// </summary>
public sealed class Notification
{
    /// <summary>
    /// Unique identifier for the notification.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Identifier of the user who owns the notification.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Required user who owns the notification.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Type of notification to send.
    /// </summary>
    public NotificationType NotificationType { get; set; }

    /// <summary>
    /// Calendar year of the event represented by this notification.
    /// </summary>
    public int EventYear { get; set; }

    /// <summary>
    /// UTC timestamp when the notification becomes due.
    /// </summary>
    public DateTimeOffset ScheduledAtUtc { get; set; }

    /// <summary>
    /// Current processing status for the notification.
    /// </summary>
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

    /// <summary>
    /// Number of delivery attempts made by the worker.
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// UTC timestamp of the most recent delivery attempt.
    /// </summary>
    public DateTimeOffset? LastAttemptAtUtc { get; set; }

    /// <summary>
    /// UTC timestamp when the notification was successfully sent.
    /// </summary>
    public DateTimeOffset? SentAtUtc { get; set; }

    /// <summary>
    /// Most recent delivery or recovery error message.
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// UTC timestamp when a failed notification becomes eligible for retry.
    /// </summary>
    public DateTimeOffset? NextRetryAtUtc { get; set; }

    /// <summary>
    /// UTC timestamp when the current processing lock expires.
    /// </summary>
    public DateTimeOffset? LockedUntilUtc { get; set; }

    /// <summary>
    /// Worker instance that currently owns the processing lock.
    /// </summary>
    public string? LockedBy { get; set; }

    /// <summary>
    /// UTC timestamp when the notification record was created.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }
}
