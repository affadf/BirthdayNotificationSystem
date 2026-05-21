using BirthdayNotificationSystem.Api.Domain;

namespace BirthdayNotificationSystem.Api.Services;

/// <summary>
/// Sends rendered notification messages to an external delivery service.
/// </summary>
public interface IMessageSender
{
    /// <summary>
    /// Sends a message for a specific user and notification record.
    /// </summary>
    /// <param name="user">The recipient user.</param>
    /// <param name="notification">The notification being delivered.</param>
    /// <param name="message">The rendered notification message body.</param>
    /// <param name="cancellationToken">Stops the operation if the worker is shutting down.</param>
    Task SendMessageAsync(User user, Notification notification, string message, CancellationToken cancellationToken);
}
