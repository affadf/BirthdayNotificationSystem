using BirthdayNotificationSystem.Api.Contracts;
using BirthdayNotificationSystem.Api.Data;
using BirthdayNotificationSystem.Api.Domain;
using BirthdayNotificationSystem.Api.Services;
using Microsoft.EntityFrameworkCore;

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
            FirstName = "Ava",
            LastName = "Taylor",
            Email = "ava.taylor@example.com",
            Birthday = new DateOnly(1992, 5, 19),
            AnniversaryDate = new DateOnly(2018, 9, 10),
            TimeZoneId = "Australia/Melbourne",
            LocationText = "Melbourne, Australia"
        }, CancellationToken.None);

        var user = await dbContext.Users.SingleAsync();
        var notifications = await dbContext.Notifications
            .OrderBy(notification => notification.NotificationType)
            .ToListAsync();

        Assert.Equal(user.Id, response.Id);
        Assert.Equal(2, notifications.Count);
        Assert.All(notifications, notification =>
        {
            Assert.Equal(user.Id, notification.UserId);
            Assert.Equal(NotificationStatus.Pending, notification.Status);
        });
        Assert.Contains(notifications, notification => notification.NotificationType == NotificationType.Birthday);
        Assert.Contains(notifications, notification => notification.NotificationType == NotificationType.Anniversary);
        Assert.Equal(2, response.UpcomingNotifications.Count);
    }

    [Fact]
    public async Task ScheduleNextAsync_DoesNotCreateDuplicateActiveNotificationForSameYearAndType()
    {
        await using var dbContext = CreateDbContext();
        var scheduler = new NotificationScheduler(dbContext, new TimeZoneService());
        var user = new User
        {
            FirstName = "Ava",
            LastName = "Taylor",
            Email = "ava.taylor@example.com",
            Birthday = new DateOnly(1992, 5, 19),
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

        Assert.Equal(1, await dbContext.Notifications.CountAsync());
    }

    private static BirthdayNotificationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BirthdayNotificationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BirthdayNotificationDbContext(options);
    }
}
