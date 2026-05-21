namespace BirthdayNotificationSystem.Api.Services;

/// <summary>
/// Calculates annual notification times at 9:00 AM in each user's configured time zone.
/// </summary>
public sealed class TimeZoneService : ITimeZoneService
{
    private static readonly TimeOnly NotificationSendTime = new(9, 0);

    /// <summary>
    /// Resolves a supplied time zone ID or throws a validation exception when it is unknown.
    /// </summary>
    /// <param name="timeZoneId">The IANA or platform-supported time zone identifier.</param>
    /// <returns>The matching time zone information.</returns>
    public TimeZoneInfo GetTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            throw InvalidTimeZone(timeZoneId);
        }
        catch (InvalidTimeZoneException)
        {
            throw InvalidTimeZone(timeZoneId);
        }
    }

    /// <summary>
    /// Calculates the next annual notification after the supplied UTC timestamp.
    /// </summary>
    /// <param name="eventDate">The annual event date.</param>
    /// <param name="timeZoneId">The user's time zone identifier.</param>
    /// <param name="utcNow">The current UTC timestamp.</param>
    /// <returns>The next event year and UTC send time.</returns>
    public NotificationSchedule CalculateNextAnnualNotification(
        DateOnly eventDate,
        string timeZoneId,
        DateTimeOffset utcNow)
    {
        var timeZone = GetTimeZone(timeZoneId);
        var localNow = TimeZoneInfo.ConvertTime(utcNow, timeZone);
        var eventYear = localNow.Year;
        var scheduled = CalculateAnnualNotificationForYear(eventDate, timeZone, eventYear);

        if (scheduled <= utcNow)
        {
            eventYear++;
            scheduled = CalculateAnnualNotificationForYear(eventDate, timeZone, eventYear);
        }

        return new NotificationSchedule(eventYear, scheduled);
    }

    /// <summary>
    /// Calculates the annual notification time for a specific event year.
    /// </summary>
    /// <param name="eventDate">The annual event date.</param>
    /// <param name="timeZoneId">The user's time zone identifier.</param>
    /// <param name="eventYear">The event year.</param>
    /// <returns>The event year and UTC send time.</returns>
    public NotificationSchedule CalculateAnnualNotificationForYear(DateOnly eventDate, string timeZoneId, int eventYear)
    {
        var timeZone = GetTimeZone(timeZoneId);
        return new NotificationSchedule(eventYear, CalculateAnnualNotificationForYear(eventDate, timeZone, eventYear));
    }

    /// <summary>
    /// Converts an event year and local 9:00 AM send time into UTC for a concrete time zone.
    /// </summary>
    /// <param name="eventDate">The annual event date.</param>
    /// <param name="timeZone">The user's resolved time zone.</param>
    /// <param name="eventYear">The event year.</param>
    /// <returns>The UTC instant when the notification should be sent.</returns>
    private static DateTimeOffset CalculateAnnualNotificationForYear(
        DateOnly eventDate,
        TimeZoneInfo timeZone,
        int eventYear)
    {
        var resolvedDate = ResolveAnnualEventDate(eventDate, eventYear);
        var localDateTime = resolvedDate.ToDateTime(NotificationSendTime, DateTimeKind.Unspecified);

        if (timeZone.IsInvalidTime(localDateTime))
        {
            localDateTime = localDateTime.AddHours(1);
        }

        var offset = timeZone.GetUtcOffset(localDateTime);
        return new DateTimeOffset(localDateTime, offset).ToUniversalTime();
    }

    /// <summary>
    /// Resolves an annual event date, sending February 29 events on February 28 in non-leap years.
    /// </summary>
    /// <param name="eventDate">The annual event date.</param>
    /// <param name="eventYear">The event year.</param>
    /// <returns>The date to use for the notification in the event year.</returns>
    private static DateOnly ResolveAnnualEventDate(DateOnly eventDate, int eventYear)
    {
        if (eventDate.Month == 2 && eventDate.Day == 29 && !DateTime.IsLeapYear(eventYear))
        {
            return new DateOnly(eventYear, 2, 28);
        }

        return new DateOnly(eventYear, eventDate.Month, eventDate.Day);
    }

    /// <summary>
    /// Builds a validation exception for an unsupported time zone ID.
    /// </summary>
    /// <param name="timeZoneId">The invalid time zone identifier.</param>
    /// <returns>A validation exception containing a field-specific error message.</returns>
    private static UserInputException InvalidTimeZone(string timeZoneId)
    {
        return new UserInputException(new Dictionary<string, string[]>
        {
            ["timeZoneId"] =
            [
                $"'{timeZoneId}' is not a valid time zone. Use an IANA ID such as 'America/New_York' or 'Australia/Melbourne'."
            ]
        });
    }
}
