using BirthdayNotificationSystem.Api.Data;
using BirthdayNotificationSystem.Api.Domain;
using BirthdayNotificationSystem.Api.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BirthdayNotificationSystem.Api.Services;

/// <summary>
/// Dispatches due notifications using atomic database claiming and retry scheduling.
/// </summary>
public sealed class NotificationDispatcher(
    BirthdayNotificationDbContext dbContext,
    IMessageSender messageSender,
    IMessageTemplateService messageTemplateService,
    INotificationScheduler notificationScheduler,
    IOptions<WorkerOptions> options,
    ILogger<NotificationDispatcher> logger) : INotificationDispatcher
{
    private readonly string _lockedBy = $"{Environment.MachineName}-{Guid.NewGuid():N}";
    private readonly WorkerOptions _options = options.Value;

    /// <summary>
    /// Finds due pending or retrying notifications and attempts to send them in batches.
    /// </summary>
    /// <param name="cancellationToken">Stops the operation if the worker is shutting down.</param>
    /// <returns>The number of notifications sent successfully.</returns>
    public async Task<int> DispatchDueNotificationsAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var dueIds = await dbContext.Notifications
            .Where(notification =>
                notification.ScheduledAtUtc <= now &&
                (notification.NextRetryAtUtc == null || notification.NextRetryAtUtc <= now) &&
                (notification.Status == NotificationStatus.Pending ||
                 notification.Status == NotificationStatus.Failed))
            .OrderBy(notification => notification.ScheduledAtUtc)
            .Take(_options.BatchSize)
            .Select(notification => notification.Id)
            .ToListAsync(cancellationToken);

        var sentCount = 0;

        foreach (var notificationId in dueIds)
        {
            if (await TryDispatchAsync(notificationId, cancellationToken))
            {
                sentCount++;
            }
        }

        return sentCount;
    }

    /// <summary>
    /// Atomically claims a single due notification, sends it, and records the final delivery or retry state.
    /// </summary>
    /// <param name="notificationId">The notification to claim and dispatch.</param>
    /// <param name="cancellationToken">Stops the operation if the worker is shutting down.</param>
    /// <returns>True when the notification was sent successfully; otherwise false.</returns>
    private async Task<bool> TryDispatchAsync(Guid notificationId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var lockedUntil = now.AddMinutes(_options.LockTimeoutMinutes);

        var notificationToSend = await dbContext.Notifications
            .Where(notification =>
                notification.Id == notificationId &&
                notification.ScheduledAtUtc <= now &&
                (notification.NextRetryAtUtc == null || notification.NextRetryAtUtc <= now) &&
                (notification.Status == NotificationStatus.Pending ||
                 notification.Status == NotificationStatus.Failed))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(notification => notification.Status, NotificationStatus.Processing)
                .SetProperty(notification => notification.LockedBy, _lockedBy)
                .SetProperty(notification => notification.LockedUntilUtc, lockedUntil)
                .SetProperty(notification => notification.LastAttemptAtUtc, now)
                .SetProperty(notification => notification.AttemptCount, notification => notification.AttemptCount + 1),
                cancellationToken);

        if (notificationToSend == 0)
        {
            return false;
        }

        var notification = await dbContext.Notifications
            .Include(candidate => candidate.User)
            .FirstAsync(candidate => candidate.Id == notificationId, cancellationToken);

        if (notification.User.IsDeleted)
        {
            await MarkCanceledAsync(notification, "User is deleted.", cancellationToken);
            return false;
        }

        try
        {
            var message = messageTemplateService.BuildMessage(notification.User, notification.NotificationType);
            await messageSender.SendMessageAsync(notification.User, notification, message, cancellationToken);
            await MarkSentAsync(notification, cancellationToken);
            await notificationScheduler.ScheduleForYearAsync(
                notification.User,
                notification.NotificationType,
                notification.EventYear + 1,
                cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Notification {NotificationId} failed.", notification.Id);
            await MarkFailedAsync(notification, ex, cancellationToken);
            return false;
        }
    }

    /// <summary>
    /// Marks a claimed notification as sent and clears retry and lock metadata.
    /// </summary>
    /// <param name="notification">The claimed notification that was sent successfully.</param>
    /// <param name="cancellationToken">Stops the operation if the worker is shutting down.</param>
    private async Task MarkSentAsync(Notification notification, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        await dbContext.Notifications
            .Where(candidate => candidate.Id == notification.Id && candidate.Status == NotificationStatus.Processing)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(candidate => candidate.Status, NotificationStatus.Sent)
                .SetProperty(candidate => candidate.SentAtUtc, now)
                .SetProperty(candidate => candidate.LastError, (string?)null)
                .SetProperty(candidate => candidate.NextRetryAtUtc, (DateTimeOffset?)null)
                .SetProperty(candidate => candidate.LockedBy, (string?)null)
                .SetProperty(candidate => candidate.LockedUntilUtc, (DateTimeOffset?)null),
                cancellationToken);
    }

    /// <summary>
    /// Marks a claimed notification as canceled when the associated user can no longer receive it.
    /// </summary>
    /// <param name="notification">The claimed notification to cancel.</param>
    /// <param name="reason">The reason stored on the notification.</param>
    /// <param name="cancellationToken">Stops the operation if the worker is shutting down.</param>
    private async Task MarkCanceledAsync(Notification notification, string reason, CancellationToken cancellationToken)
    {
        await dbContext.Notifications
            .Where(candidate => candidate.Id == notification.Id && candidate.Status == NotificationStatus.Processing)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(candidate => candidate.Status, NotificationStatus.Canceled)
                .SetProperty(candidate => candidate.LastError, reason)
                .SetProperty(candidate => candidate.LockedBy, (string?)null)
                .SetProperty(candidate => candidate.LockedUntilUtc, (DateTimeOffset?)null),
                cancellationToken);
    }

    /// <summary>
    /// Marks a claimed notification as failed and sets the next database-level retry time.
    /// </summary>
    /// <param name="notification">The claimed notification that failed to send.</param>
    /// <param name="exception">The exception raised by the email dispatch attempt.</param>
    /// <param name="cancellationToken">Stops the operation if the worker is shutting down.</param>
    private async Task MarkFailedAsync(Notification notification, Exception exception, CancellationToken cancellationToken)
    {
        var retryAtUtc = DateTimeOffset.UtcNow.Add(GetRetryDelay(notification.AttemptCount));
        var error = exception.GetBaseException().Message;
        var storedError = error.Length <= 2000 ? error : error.Substring(0, 2000);

        await dbContext.Notifications
            .Where(candidate => candidate.Id == notification.Id && candidate.Status == NotificationStatus.Processing)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(candidate => candidate.Status, NotificationStatus.Failed)
                .SetProperty(candidate => candidate.LastError, storedError)
                .SetProperty(candidate => candidate.NextRetryAtUtc, retryAtUtc)
                .SetProperty(candidate => candidate.LockedBy, (string?)null)
                .SetProperty(candidate => candidate.LockedUntilUtc, (DateTimeOffset?)null),
                cancellationToken);
    }

    /// <summary>
    /// Calculates the longer database retry delay based on the total notification attempt count.
    /// </summary>
    /// <param name="attemptCount">The current attempt count stored on the notification.</param>
    /// <returns>The delay before the notification should be retried.</returns>
    private static TimeSpan GetRetryDelay(int attemptCount)
    {
        return attemptCount switch
        {
            <= 1 => TimeSpan.FromMinutes(1),
            2 => TimeSpan.FromMinutes(5),
            3 => TimeSpan.FromMinutes(15),
            4 => TimeSpan.FromHours(1),
            _ => TimeSpan.FromHours(6)
        };
    }
}
