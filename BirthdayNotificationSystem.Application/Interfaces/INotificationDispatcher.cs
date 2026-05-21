namespace BirthdayNotificationSystem.Application.Interfaces;

/// <summary>
/// Sends due notification records and updates their delivery state.
/// </summary>
public interface INotificationDispatcher
{
    /// <summary>
    /// Claims due notifications, sends messages, and records success or retry state.
    /// </summary>
    /// <param name="cancellationToken">Stops the operation if the worker is shutting down.</param>
    /// <returns>The number of notifications sent successfully.</returns>
    Task<int> DispatchDueNotificationsAsync(CancellationToken cancellationToken);
}
