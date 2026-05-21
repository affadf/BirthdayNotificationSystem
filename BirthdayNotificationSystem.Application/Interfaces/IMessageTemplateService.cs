using BirthdayNotificationSystem.Domain;

namespace BirthdayNotificationSystem.Application.Interfaces;

/// <summary>
/// Builds message bodies for notification types.
/// </summary>
public interface IMessageTemplateService
{
    /// <summary>
    /// Builds the required message text for a user and notification type.
    /// </summary>
    /// <param name="user">The user whose name appears in the message.</param>
    /// <param name="notificationType">The notification type being delivered.</param>
    /// <returns>The notification message body.</returns>
    string BuildMessage(User user, NotificationType notificationType);
}
