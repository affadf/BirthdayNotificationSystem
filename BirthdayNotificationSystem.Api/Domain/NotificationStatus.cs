namespace BirthdayNotificationSystem.Api.Domain;

/// <summary>
/// Processing state for a durable notification record.
/// </summary>
public enum NotificationStatus
{
    /// <summary>
    /// Notification is waiting for its scheduled time or retry time.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Notification has been claimed by a worker and is being sent.
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Notification was sent successfully.
    /// </summary>
    Sent = 2,

    /// <summary>
    /// Notification failed and may be retried later.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Notification was canceled because the user changed or was deleted.
    /// </summary>
    Canceled = 4
}
