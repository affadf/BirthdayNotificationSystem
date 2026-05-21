namespace BirthdayNotificationSystem.Domain;

/// <summary>
/// Represents a calculated notification schedule.
/// </summary>
/// <param name="EventYear">The event year.</param>
/// <param name="ScheduledAtUtc">The UTC instant when the notification should be sent.</param>
public sealed record NotificationSchedule(int EventYear, DateTimeOffset ScheduledAtUtc);
