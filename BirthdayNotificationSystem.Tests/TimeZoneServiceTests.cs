using BirthdayNotificationSystem.Application.Services;
using FluentAssertions;

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

        schedule.EventYear.Should().Be(2026);
        schedule.ScheduledAtUtc.Should().Be(new DateTimeOffset(2026, 5, 19, 13, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void CalculateNextAnnualNotification_UsesUsersLocalNineAmForMelbourne()
    {
        var birthday = new DateOnly(1992, 5, 19);
        var utcNow = new DateTimeOffset(2026, 5, 18, 22, 59, 0, TimeSpan.Zero);

        var schedule = _service.CalculateNextAnnualNotification(birthday, "Australia/Melbourne", utcNow);

        schedule.EventYear.Should().Be(2026);
        schedule.ScheduledAtUtc.Should().Be(new DateTimeOffset(2026, 5, 18, 23, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void CalculateAnnualNotificationForYear_SendsFebruaryTwentyNinthEventsOnFebruaryTwentyEighthInNonLeapYears()
    {
        var birthday = new DateOnly(1992, 2, 29);

        var schedule = _service.CalculateAnnualNotificationForYear(birthday, "America/New_York", 2027);

        schedule.EventYear.Should().Be(2027);
        schedule.ScheduledAtUtc.Should().Be(new DateTimeOffset(2027, 2, 28, 14, 0, 0, TimeSpan.Zero));
    }
}
