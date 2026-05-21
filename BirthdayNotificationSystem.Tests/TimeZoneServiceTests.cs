using BirthdayNotificationSystem.Api.Services;

namespace BirthdayNotificationSystem.Tests;

public sealed class TimeZoneServiceTests
{
    private readonly TimeZoneService _service = new();

    [Fact]
    public void CalculateNextAnnualNotification_UsesUsersLocalNineAmForNewYork()
    {
        var birthday = new DateOnly(1992, 5, 19);
        var utcNow = new DateTimeOffset(2026, 5, 19, 12, 59, 0, TimeSpan.Zero);

        var schedule = _service.CalculateNextAnnualNotification(birthday, "America/New_York", utcNow);

        Assert.Equal(2026, schedule.EventYear);
        Assert.Equal(new DateTimeOffset(2026, 5, 19, 13, 0, 0, TimeSpan.Zero), schedule.ScheduledAtUtc);
    }

    [Fact]
    public void CalculateNextAnnualNotification_UsesUsersLocalNineAmForMelbourne()
    {
        var birthday = new DateOnly(1992, 5, 19);
        var utcNow = new DateTimeOffset(2026, 5, 18, 22, 59, 0, TimeSpan.Zero);

        var schedule = _service.CalculateNextAnnualNotification(birthday, "Australia/Melbourne", utcNow);

        Assert.Equal(2026, schedule.EventYear);
        Assert.Equal(new DateTimeOffset(2026, 5, 18, 23, 0, 0, TimeSpan.Zero), schedule.ScheduledAtUtc);
    }

    [Fact]
    public void CalculateAnnualNotificationForYear_SendsFebruaryTwentyNinthEventsOnFebruaryTwentyEighthInNonLeapYears()
    {
        var birthday = new DateOnly(1992, 2, 29);

        var schedule = _service.CalculateAnnualNotificationForYear(birthday, "America/New_York", 2027);

        Assert.Equal(2027, schedule.EventYear);
        Assert.Equal(new DateTimeOffset(2027, 2, 28, 14, 0, 0, TimeSpan.Zero), schedule.ScheduledAtUtc);
    }
}
