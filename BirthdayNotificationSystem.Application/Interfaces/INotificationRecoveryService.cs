namespace BirthdayNotificationSystem.Application.Interfaces;

/// <summary>
/// Repairs notification state after worker failures, expired locks, or missing schedules.
/// </summary>
public interface INotificationRecoveryService
{
    /// <summary>
    /// Returns processing notifications with expired locks to a retryable failed state.
    /// </summary>
    /// <param name="cancellationToken">Stops the operation if the worker is shutting down.</param>
    /// <returns>The number of recovered notifications.</returns>
    Task<int> RecoverExpiredLocksAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Ensures active users have applicable upcoming notifications scheduled.
    /// </summary>
    /// <param name="cancellationToken">Stops the operation if the worker is shutting down.</param>
    /// <returns>The number of missing schedules created.</returns>
    Task<int> EnsureUpcomingNotificationSchedulesAsync(CancellationToken cancellationToken);
}
