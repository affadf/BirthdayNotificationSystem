using System.Net.Mail;
using BirthdayNotificationSystem.Application.Contracts;
using BirthdayNotificationSystem.Application.Exceptions;
using BirthdayNotificationSystem.Application.Interfaces;
using BirthdayNotificationSystem.Domain;
using Microsoft.EntityFrameworkCore;

namespace BirthdayNotificationSystem.Application.Services;

/// <summary>
/// Coordinates user create, update, and delete workflows with durable notification scheduling.
/// </summary>
public sealed class UserService(
    IApplicationDbContext dbContext,
    INotificationScheduler notificationScheduler,
    ITimeZoneService timeZoneService) : IUserService
{
    /// <summary>
    /// Validates and creates a user, then adds applicable upcoming notifications in the same unit of work.
    /// </summary>
    /// <param name="request">The user details to create.</param>
    /// <param name="cancellationToken">Stops the operation if the request is canceled.</param>
    /// <returns>The created user response with upcoming notification schedules.</returns>
    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        ValidateBirthday(request.Birthday);
        ValidateAnniversaryDate(request.AnniversaryDate);
        timeZoneService.GetTimeZone(request.TimeZoneId);

        var email = request.Email.Trim();
        ValidateEmail(email);
        await EnsureEmailIsAvailableAsync(email, null, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            Birthday = request.Birthday,
            AnniversaryDate = request.AnniversaryDate,
            TimeZoneId = request.TimeZoneId.Trim(),
            LocationText = request.LocationText?.Trim() ?? string.Empty,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        dbContext.Users.Add(user);
        await notificationScheduler.ScheduleUpcomingNotificationsAsync(user, now, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await MapAsync(user, cancellationToken);
    }

    /// <summary>
    /// Updates an active user, cancels pending future notifications, and schedules recalculated upcoming notifications.
    /// </summary>
    /// <param name="id">The user identifier to update.</param>
    /// <param name="request">The replacement user details.</param>
    /// <param name="cancellationToken">Stops the operation if the request is canceled.</param>
    /// <returns>The updated user response, or null when no active user matches the ID.</returns>
    public async Task<UserResponse?> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        ValidateBirthday(request.Birthday);
        ValidateAnniversaryDate(request.AnniversaryDate);
        timeZoneService.GetTimeZone(request.TimeZoneId);

        var user = await dbContext.Users.FirstOrDefaultAsync(
            candidate => candidate.Id == id && !candidate.IsDeleted,
            cancellationToken);

        if (user is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var email = request.Email.Trim();
        ValidateEmail(email);
        await EnsureEmailIsAvailableAsync(email, user.Id, cancellationToken);

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Email = email;
        user.Birthday = request.Birthday;
        user.AnniversaryDate = request.AnniversaryDate;
        user.TimeZoneId = request.TimeZoneId.Trim();
        user.LocationText = request.LocationText?.Trim() ?? string.Empty;
        user.UpdatedAtUtc = now;

        await CancelFutureNotificationsAsync(user.Id, now, cancellationToken);
        await notificationScheduler.ScheduleUpcomingNotificationsAsync(user, now, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await MapAsync(user, cancellationToken);
    }

    /// <summary>
    /// Soft-deletes an active user and cancels pending future notifications while preserving history.
    /// </summary>
    /// <param name="id">The user identifier to delete.</param>
    /// <param name="cancellationToken">Stops the operation if the request is canceled.</param>
    /// <returns>True when the user was found and deleted; otherwise false.</returns>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(
            candidate => candidate.Id == id && !candidate.IsDeleted,
            cancellationToken);

        if (user is null)
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        user.IsDeleted = true;
        user.UpdatedAtUtc = now;

        await CancelFutureNotificationsAsync(user.Id, now, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Cancels pending or retrying notifications scheduled in the future for a user.
    /// </summary>
    /// <param name="userId">The user whose future notifications should be canceled.</param>
    /// <param name="utcNow">The current UTC timestamp used to identify future notifications.</param>
    /// <param name="cancellationToken">Stops the operation if the request is canceled.</param>
    private async Task CancelFutureNotificationsAsync(
        Guid userId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        await dbContext.Notifications
            .Where(notification =>
                notification.UserId == userId &&
                notification.ScheduledAtUtc >= utcNow &&
                (notification.Status == NotificationStatus.Pending ||
                 notification.Status == NotificationStatus.Failed))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(notification => notification.Status, NotificationStatus.Canceled)
                .SetProperty(notification => notification.LastError, "Canceled because the user was updated or deleted.")
                .SetProperty(notification => notification.LockedBy, (string?)null)
                .SetProperty(notification => notification.LockedUntilUtc, (DateTimeOffset?)null),
                cancellationToken);
    }

    /// <summary>
    /// Builds the API response for a user and includes upcoming active notification schedules.
    /// </summary>
    /// <param name="user">The user entity to map.</param>
    /// <param name="cancellationToken">Stops the operation if the request is canceled.</param>
    /// <returns>The user response returned by the API.</returns>
    private async Task<UserResponse> MapAsync(User user, CancellationToken cancellationToken)
    {
        var upcomingNotifications = await dbContext.Notifications
            .Where(notification =>
                notification.UserId == user.Id &&
                (notification.Status == NotificationStatus.Pending ||
                 notification.Status == NotificationStatus.Failed ||
                 notification.Status == NotificationStatus.Processing))
            .OrderBy(notification => notification.ScheduledAtUtc)
            .Select(notification => new NotificationScheduleResponse(
                notification.NotificationType,
                notification.ScheduledAtUtc))
            .ToListAsync(cancellationToken);

        return new UserResponse(
            user.Id,
            user.FirstName,
            user.LastName,
            user.FullName,
            user.Email,
            user.Birthday,
            user.AnniversaryDate,
            user.TimeZoneId,
            user.LocationText,
            user.CreatedAtUtc,
            user.UpdatedAtUtc,
            upcomingNotifications);
    }

    /// <summary>
    /// Ensures no other user already uses the supplied email address.
    /// </summary>
    /// <param name="email">The trimmed email address to validate.</param>
    /// <param name="currentUserId">The current user ID to ignore during updates.</param>
    /// <param name="cancellationToken">Stops the operation if the request is canceled.</param>
    /// <exception cref="UserInputException">Thrown when the email address already exists.</exception>
    private async Task EnsureEmailIsAvailableAsync(
        string email,
        Guid? currentUserId,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.ToUpperInvariant();
        var exists = await dbContext.Users.AnyAsync(user =>
            user.Email.ToUpper() == normalizedEmail &&
            (currentUserId == null || user.Id != currentUserId),
            cancellationToken);

        if (exists)
        {
            throw new UserInputException(new Dictionary<string, string[]>
            {
                ["email"] = ["Email address already exists."]
            });
        }
    }

    /// <summary>
    /// Ensures the email address has a valid email format.
    /// </summary>
    /// <param name="email">The trimmed email address supplied by the API caller.</param>
    /// <exception cref="UserInputException">Thrown when the email address format is invalid.</exception>
    private static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw InvalidEmail();
        }

        try
        {
            var parsedEmail = new MailAddress(email);
            if (!string.Equals(parsedEmail.Address, email, StringComparison.OrdinalIgnoreCase))
            {
                throw InvalidEmail();
            }
        }
        catch (FormatException)
        {
            throw InvalidEmail();
        }
    }

    private static UserInputException InvalidEmail()
    {
        return new UserInputException(new Dictionary<string, string[]>
        {
            ["email"] = ["Email address format is invalid."]
        });
    }

    /// <summary>
    /// Ensures the birthday value is present and not in the future.
    /// </summary>
    /// <param name="birthday">The birthday supplied by the API caller.</param>
    /// <exception cref="UserInputException">Thrown when the birthday is invalid.</exception>
    private static void ValidateBirthday(DateOnly birthday)
    {
        if (birthday == default || birthday > DateOnly.FromDateTime(DateTime.UtcNow.Date))
        {
            throw new UserInputException(new Dictionary<string, string[]>
            {
                ["birthday"] = ["Birthday must be a valid date that is not in the future."]
            });
        }
    }

    /// <summary>
    /// Ensures the optional anniversary date is not in the future.
    /// </summary>
    /// <param name="anniversaryDate">The optional anniversary date supplied by the API caller.</param>
    /// <exception cref="UserInputException">Thrown when the anniversary date is invalid.</exception>
    private static void ValidateAnniversaryDate(DateOnly? anniversaryDate)
    {
        if (anniversaryDate > DateOnly.FromDateTime(DateTime.UtcNow.Date))
        {
            throw new UserInputException(new Dictionary<string, string[]>
            {
                ["anniversaryDate"] = ["Anniversary date must not be in the future."]
            });
        }
    }
}
