using BirthdayNotificationSystem.Application.Contracts;
using BirthdayNotificationSystem.Application.Exceptions;
using BirthdayNotificationSystem.Application.Interfaces;
using BirthdayNotificationSystem.Application.Services;
using BirthdayNotificationSystem.Domain;
using BirthdayNotificationSystem.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace BirthdayNotificationSystem.Tests;

public sealed class UserServiceTests
{
    [Fact]
    public async Task CreateAsync_PersistsUserAndSchedulesApplicableNotifications()
    {
        await using var dbContext = CreateDbContext();
        var timeZoneService = new TimeZoneService();
        var scheduler = new NotificationScheduler(dbContext, timeZoneService);
        var service = new UserService(dbContext, scheduler, timeZoneService);

        var response = await service.CreateAsync(new CreateUserRequest
        {
            FirstName = "Affan",
            LastName = "Daf",
            Email = "affan.daf@example.com",
            Birthday = new DateOnly(1990, 5, 19),
            AnniversaryDate = new DateOnly(2018, 9, 10),
            TimeZoneId = "Australia/Melbourne",
            LocationText = "Melbourne, Australia"
        }, CancellationToken.None);

        var user = await dbContext.Users.SingleAsync();
        var notifications = await dbContext.Notifications
            .OrderBy(notification => notification.NotificationType)
            .ToListAsync();

        response.Id.Should().Be(user.Id);
        notifications.Should().HaveCount(2);
        notifications.Should().OnlyContain(notification =>
            notification.UserId == user.Id &&
            notification.Status == NotificationStatus.Pending);
        notifications.Should().Contain(notification => notification.NotificationType == NotificationType.Birthday);
        notifications.Should().Contain(notification => notification.NotificationType == NotificationType.Anniversary);
        response.UpcomingNotifications.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_ThrowsValidationErrorWhenEmailFormatIsInvalid()
    {
        await using var dbContext = CreateDbContext();
        var scheduler = Substitute.For<INotificationScheduler>();
        var timeZoneService = Substitute.For<ITimeZoneService>();
        timeZoneService.GetTimeZone("Australia/Melbourne")
            .Returns(TimeZoneInfo.FindSystemTimeZoneById("Australia/Melbourne"));
        var service = new UserService(dbContext, scheduler, timeZoneService);

        var act = async () => await service.CreateAsync(new CreateUserRequest
        {
            FirstName = "Affan",
            LastName = "Daf",
            Email = "not-an-email",
            Birthday = new DateOnly(1990, 5, 19),
            TimeZoneId = "Australia/Melbourne",
            LocationText = "Melbourne, Australia"
        }, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<UserInputException>();
        exception.Which.Errors.Should().ContainKey("email")
            .WhoseValue.Should().ContainSingle("Email address format is invalid.");
        await scheduler.DidNotReceiveWithAnyArgs()
            .ScheduleUpcomingNotificationsAsync(default!, default, default);
    }

    [Fact]
    public async Task CreateAsync_ThrowsValidationErrorWhenEmailAlreadyExists()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Users.Add(new User
        {
            FirstName = "Affan",
            LastName = "Daf",
            Email = "affan.daf@example.com",
            Birthday = new DateOnly(1990, 5, 19),
            TimeZoneId = "Australia/Melbourne",
            LocationText = "Melbourne, Australia",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var scheduler = Substitute.For<INotificationScheduler>();
        var timeZoneService = Substitute.For<ITimeZoneService>();
        timeZoneService.GetTimeZone("Australia/Melbourne")
            .Returns(TimeZoneInfo.FindSystemTimeZoneById("Australia/Melbourne"));
        var service = new UserService(dbContext, scheduler, timeZoneService);

        var act = async () => await service.CreateAsync(new CreateUserRequest
        {
            FirstName = "Another",
            LastName = "User",
            Email = "AFFAN.DAF@example.com",
            Birthday = new DateOnly(1995, 3, 20),
            TimeZoneId = "Australia/Melbourne",
            LocationText = "Melbourne, Australia"
        }, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<UserInputException>();
        exception.Which.Errors.Should().ContainKey("email")
            .WhoseValue.Should().ContainSingle("Email address already exists.");
        await scheduler.DidNotReceiveWithAnyArgs()
            .ScheduleUpcomingNotificationsAsync(default!, default, default);
    }

    [Fact]
    public async Task CreateAsync_UsesSchedulerForCreatedUser()
    {
        await using var dbContext = CreateDbContext();
        var scheduler = Substitute.For<INotificationScheduler>();
        var timeZoneService = Substitute.For<ITimeZoneService>();
        timeZoneService.GetTimeZone("Australia/Melbourne")
            .Returns(TimeZoneInfo.FindSystemTimeZoneById("Australia/Melbourne"));
        var service = new UserService(dbContext, scheduler, timeZoneService);

        var response = await service.CreateAsync(new CreateUserRequest
        {
            FirstName = "Affan",
            LastName = "Daf",
            Email = "affan.daf@example.com",
            Birthday = new DateOnly(1990, 5, 19),
            AnniversaryDate = new DateOnly(2018, 9, 10),
            TimeZoneId = "Australia/Melbourne",
            LocationText = "Melbourne, Australia"
        }, CancellationToken.None);

        var user = await dbContext.Users.SingleAsync();

        response.Id.Should().Be(user.Id);
        await scheduler.Received(1)
            .ScheduleUpcomingNotificationsAsync(
                Arg.Is<User>(candidate => candidate.Id == user.Id),
                Arg.Any<DateTimeOffset>(),
                CancellationToken.None);
    }

    [Fact]
    public async Task ScheduleNextAsync_DoesNotCreateDuplicateActiveNotificationForSameYearAndType()
    {
        await using var dbContext = CreateDbContext();
        var scheduler = new NotificationScheduler(dbContext, new TimeZoneService());
        var user = new User
        {
			FirstName = "Affan",
			LastName = "Daf",
			Email = "affan.daf@example.com",
			Birthday = new DateOnly(1990, 5, 19),
            TimeZoneId = "America/New_York",
            LocationText = "New York",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.Users.Add(user);
        var utcNow = new DateTimeOffset(2026, 5, 19, 12, 0, 0, TimeSpan.Zero);

        await scheduler.ScheduleNextAsync(user, NotificationType.Birthday, utcNow, CancellationToken.None);
        await dbContext.SaveChangesAsync();
        await scheduler.ScheduleNextAsync(user, NotificationType.Birthday, utcNow, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        var notificationCount = await dbContext.Notifications.CountAsync();
        notificationCount.Should().Be(1);
    }

    private static BirthdayNotificationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BirthdayNotificationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BirthdayNotificationDbContext(options);
    }
}
