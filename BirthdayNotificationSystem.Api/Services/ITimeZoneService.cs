namespace BirthdayNotificationSystem.Api.Services;

/// <summary>
/// Provides time-zone validation and annual notification scheduling calculations.
/// </summary>
public interface ITimeZoneService
{
    /// <summary>
    /// Resolves and validates a time zone identifier.
    /// </summary>
    /// <param name="timeZoneId">The IANA or platform-supported time zone identifier.</param>
    /// <returns>The matching time zone information.</returns>
    TimeZoneInfo GetTimeZone(string timeZoneId);

    /// <summary>
    /// Calculates the next annual notification send time after the supplied UTC timestamp.
    /// </summary>
    /// <param name="eventDate">The annual event date.</param>
    /// <param name="timeZoneId">The user's time zone identifier.</param>
    /// <param name="utcNow">The current UTC timestamp.</param>
    /// <returns>The event year and scheduled UTC send time.</returns>
    NotificationSchedule CalculateNextAnnualNotification(DateOnly eventDate, string timeZoneId, DateTimeOffset utcNow);

    /// <summary>
    /// Calculates an annual notification send time for a specific event year.
    /// </summary>
    /// <param name="eventDate">The annual event date.</param>
    /// <param name="timeZoneId">The user's time zone identifier.</param>
    /// <param name="eventYear">The year in which the notification should be sent.</param>
    /// <returns>The event year and scheduled UTC send time.</returns>
    NotificationSchedule CalculateAnnualNotificationForYear(DateOnly eventDate, string timeZoneId, int eventYear);
}

/// <summary>
/// Represents a calculated notification schedule.
/// </summary>
/// <param name="EventYear">The event year.</param>
/// <param name="ScheduledAtUtc">The UTC instant when the notification should be sent.</param>
public sealed record NotificationSchedule(int EventYear, DateTimeOffset ScheduledAtUtc);
