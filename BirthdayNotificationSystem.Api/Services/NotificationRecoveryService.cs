using BirthdayNotificationSystem.Api.Data;
using BirthdayNotificationSystem.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BirthdayNotificationSystem.Api.Services;

/// <summary>
/// Recovers stuck notification locks and backfills missing future notification schedules.
/// </summary>
public sealed class NotificationRecoveryService(
    BirthdayNotificationDbContext dbContext,
    INotificationScheduler notificationScheduler) : INotificationRecoveryService
{
    /// <summary>
    /// Finds processing notifications whose locks expired and makes them retryable again.
    /// </summary>
    /// <param name="cancellationToken">Stops the operation if the worker is shutting down.</param>
    /// <returns>The number of notifications moved back into a retryable state.</returns>
    public async Task<int> RecoverExpiredLocksAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        return await dbContext.Notifications
            .Where(notification =>
                notification.Status == NotificationStatus.Processing &&
                notification.LockedUntilUtc != null &&
                notification.LockedUntilUtc <= now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(notification => notification.Status, NotificationStatus.Failed)
                .SetProperty(notification => notification.NextRetryAtUtc, now)
                .SetProperty(notification => notification.LastError, "Processing lock expired and was recovered.")
                .SetProperty(notification => notification.LockedBy, (string?)null)
                .SetProperty(notification => notification.LockedUntilUtc, (DateTimeOffset?)null),
                cancellationToken);
    }

    /// <summary>
    /// Creates upcoming notifications for active users when applicable schedules are missing.
    /// </summary>
    /// <param name="cancellationToken">Stops the operation if the worker is shutting down.</param>
    /// <returns>The number of schedules created.</returns>
    public async Task<int> EnsureUpcomingNotificationSchedulesAsync(CancellationToken cancellationToken)
    {
        var users = await dbContext.Users
            .Where(user => !user.IsDeleted)
            .OrderBy(user => user.Id)
            .Take(500)
            .ToListAsync(cancellationToken);

        var created = 0;
        var now = DateTimeOffset.UtcNow;

        foreach (var user in users)
        {
            created += await notificationScheduler.ScheduleUpcomingNotificationsAsync(user, now, cancellationToken);
        }

        if (created > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return created;
    }
}
