using System.ComponentModel.DataAnnotations;
using BirthdayNotificationSystem.Api.Domain;

namespace BirthdayNotificationSystem.Api.Contracts;

/// <summary>
/// Request body for creating a user and scheduling applicable upcoming notifications.
/// </summary>
public sealed class CreateUserRequest
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; init; } = string.Empty;

    public DateOnly Birthday { get; init; }

    public DateOnly? AnniversaryDate { get; init; }

    [Required]
    [MaxLength(100)]
    public string TimeZoneId { get; init; } = string.Empty;

    [MaxLength(250)]
    public string? LocationText { get; init; }
}

/// <summary>
/// Request body for updating a user and recalculating notification schedules.
/// </summary>
public sealed class UpdateUserRequest
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; init; } = string.Empty;

    public DateOnly Birthday { get; init; }

    public DateOnly? AnniversaryDate { get; init; }

    [Required]
    [MaxLength(100)]
    public string TimeZoneId { get; init; } = string.Empty;

    [MaxLength(250)]
    public string? LocationText { get; init; }
}

/// <summary>
/// Response returned after creating or updating a user.
/// </summary>
/// <param name="Id">The user's unique identifier.</param>
/// <param name="FirstName">The user's first name.</param>
/// <param name="LastName">The user's last name.</param>
/// <param name="FullName">The user's combined first and last name.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="Birthday">The user's birthday date.</param>
/// <param name="AnniversaryDate">Optional anniversary date used for anniversary notifications.</param>
/// <param name="TimeZoneId">The user's time zone identifier.</param>
/// <param name="LocationText">Free-text location information supplied by the caller.</param>
/// <param name="CreatedAtUtc">When the user was created in UTC.</param>
/// <param name="UpdatedAtUtc">When the user was last updated in UTC.</param>
/// <param name="UpcomingNotifications">Upcoming active notifications grouped by notification type.</param>
public sealed record UserResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    DateOnly Birthday,
    DateOnly? AnniversaryDate,
    string TimeZoneId,
    string LocationText,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<NotificationScheduleResponse> UpcomingNotifications);

/// <summary>
/// Response describing an upcoming active notification schedule for a user.
/// </summary>
/// <param name="NotificationType">The notification type that will be sent.</param>
/// <param name="ScheduledAtUtc">The UTC send time for the notification.</param>
public sealed record NotificationScheduleResponse(NotificationType NotificationType, DateTimeOffset ScheduledAtUtc);
