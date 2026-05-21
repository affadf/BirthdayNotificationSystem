using BirthdayNotificationSystem.Api.Domain;

namespace BirthdayNotificationSystem.Api.Services;

/// <summary>
/// Creates durable notification records for users.
/// </summary>
public interface INotificationScheduler
{
    /// <summary>
    /// Schedules all applicable upcoming notifications for a user.
    /// </summary>
    /// <param name="user">The user who owns the notifications.</param>
    /// <param name="utcNow">The current UTC timestamp.</param>
    /// <param name="cancellationToken">Stops the operation if the request is canceled.</param>
    /// <returns>The number of notifications created.</returns>
    Task<int> ScheduleUpcomingNotificationsAsync(User user, DateTimeOffset utcNow, CancellationToken cancellationToken);

    /// <summary>
    /// Schedules the user's next notification of the requested type after the supplied UTC timestamp.
    /// </summary>
    /// <param name="user">The user who owns the notification.</param>
    /// <param name="notificationType">The notification type to schedule.</param>
    /// <param name="utcNow">The current UTC timestamp.</param>
    /// <param name="cancellationToken">Stops the operation if the request is canceled.</param>
    /// <returns>The created notification, or null when the type is not applicable or an active notification already exists.</returns>
    Task<Notification?> ScheduleNextAsync(
        User user,
        NotificationType notificationType,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken);

    /// <summary>
    /// Schedules a notification of the requested type for a specific event year.
    /// </summary>
    /// <param name="user">The user who owns the notification.</param>
    /// <param name="notificationType">The notification type to schedule.</param>
    /// <param name="eventYear">The event year to schedule.</param>
    /// <param name="cancellationToken">Stops the operation if the request is canceled.</param>
    /// <returns>The created notification, or null when the type is not applicable or an active notification already exists.</returns>
    Task<Notification?> ScheduleForYearAsync(
        User user,
        NotificationType notificationType,
        int eventYear,
        CancellationToken cancellationToken);
}
