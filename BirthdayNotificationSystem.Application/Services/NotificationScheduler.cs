using BirthdayNotificationSystem.Application.Interfaces;
using BirthdayNotificationSystem.Domain;
using Microsoft.EntityFrameworkCore;

namespace BirthdayNotificationSystem.Application.Services;

/// <summary>
/// Creates pending notification records while avoiding duplicate active schedules.
/// </summary>
public sealed class NotificationScheduler(
    IApplicationDbContext dbContext,
    ITimeZoneService timeZoneService) : INotificationScheduler
{
    /// <summary>
    /// Schedules every notification type that has enough user data to calculate an upcoming event date.
    /// </summary>
    /// <param name="user">The user who owns the notifications.</param>
    /// <param name="utcNow">The current UTC timestamp.</param>
    /// <param name="cancellationToken">Stops the operation if the request is canceled.</param>
    /// <returns>The number of notifications created.</returns>
    public async Task<int> ScheduleUpcomingNotificationsAsync(
        User user,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var created = 0;

        foreach (var notificationType in Enum.GetValues<NotificationType>())
        {
            var notification = await ScheduleNextAsync(user, notificationType, utcNow, cancellationToken);
            if (notification is not null)
            {
                created++;
            }
        }

        return created;
    }

    /// <summary>
    /// Calculates and stores the user's next notification of the requested type.
    /// </summary>
    /// <param name="user">The user who owns the notification.</param>
    /// <param name="notificationType">The notification type to schedule.</param>
    /// <param name="utcNow">The current UTC timestamp.</param>
    /// <param name="cancellationToken">Stops the operation if the request is canceled.</param>
    /// <returns>The created notification, or null when the type is not applicable or an active notification already exists.</returns>
    public async Task<Notification?> ScheduleNextAsync(
        User user,
        NotificationType notificationType,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var eventDate = GetEventDate(user, notificationType);
        if (eventDate is null)
        {
            return null;
        }

        var schedule = timeZoneService.CalculateNextAnnualNotification(eventDate.Value, user.TimeZoneId, utcNow);
        return await AddIfMissingAsync(user, notificationType, schedule, cancellationToken);
    }

    /// <summary>
    /// Calculates and stores a notification of the requested type for a specific event year.
    /// </summary>
    /// <param name="user">The user who owns the notification.</param>
    /// <param name="notificationType">The notification type to schedule.</param>
    /// <param name="eventYear">The event year to schedule.</param>
    /// <param name="cancellationToken">Stops the operation if the request is canceled.</param>
    /// <returns>The created notification, or null when the type is not applicable or an active notification already exists.</returns>
    public async Task<Notification?> ScheduleForYearAsync(
        User user,
        NotificationType notificationType,
        int eventYear,
        CancellationToken cancellationToken)
    {
        var eventDate = GetEventDate(user, notificationType);
        if (eventDate is null)
        {
            return null;
        }

        var schedule = timeZoneService.CalculateAnnualNotificationForYear(eventDate.Value, user.TimeZoneId, eventYear);
        return await AddIfMissingAsync(user, notificationType, schedule, cancellationToken);
    }

    /// <summary>
    /// Adds a pending notification only when no active notification already exists for the same user, type, and event year.
    /// </summary>
    /// <param name="user">The user who owns the notification.</param>
    /// <param name="notificationType">The notification type to create.</param>
    /// <param name="schedule">The calculated notification schedule.</param>
    /// <param name="cancellationToken">Stops the operation if the request is canceled.</param>
    /// <returns>The added notification, or null if a duplicate active notification was found.</returns>
    private async Task<Notification?> AddIfMissingAsync(
        User user,
        NotificationType notificationType,
        NotificationSchedule schedule,
        CancellationToken cancellationToken)
    {
        var activeStatuses = new[]
        {
            NotificationStatus.Pending,
            NotificationStatus.Processing,
            NotificationStatus.Failed
        };

        var exists = await dbContext.Notifications.AnyAsync(notification =>
            notification.UserId == user.Id &&
            notification.NotificationType == notificationType &&
            notification.EventYear == schedule.EventYear &&
            activeStatuses.Contains(notification.Status), cancellationToken);

        if (exists)
        {
            return null;
        }

        var notification = new Notification
        {
            UserId = user.Id,
            NotificationType = notificationType,
            EventYear = schedule.EventYear,
            ScheduledAtUtc = schedule.ScheduledAtUtc,
            Status = NotificationStatus.Pending,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.Notifications.Add(notification);
        return notification;
    }

    private static DateOnly? GetEventDate(User user, NotificationType notificationType)
    {
        return notificationType switch
        {
            NotificationType.Birthday => user.Birthday,
            NotificationType.Anniversary => user.AnniversaryDate,
            _ => null
        };
    }
}
