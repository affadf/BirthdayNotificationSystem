using BirthdayNotificationSystem.Api.Domain;

namespace BirthdayNotificationSystem.Api.Services;

/// <summary>
/// Creates notification message text from domain data.
/// </summary>
public sealed class MessageTemplateService : IMessageTemplateService
{
    /// <summary>
    /// Builds the required message using the user's full name and the notification type.
    /// </summary>
    /// <param name="user">The user whose full name should appear in the message.</param>
    /// <param name="notificationType">The notification type being delivered.</param>
    /// <returns>The notification message body.</returns>
    public string BuildMessage(User user, NotificationType notificationType)
    {
        return notificationType switch
        {
            NotificationType.Birthday => $"Hey, {user.FullName} it's your birthday",
            NotificationType.Anniversary => $"Hey, {user.FullName} happy anniversary",
            _ => throw new NotSupportedException($"Notification type '{notificationType}' is not supported.")
        };
    }
}
