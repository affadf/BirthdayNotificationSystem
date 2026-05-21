namespace BirthdayNotificationSystem.Domain;

/// <summary>
/// Represents a user who should receive notifications.
/// </summary>
public sealed class User
{
    /// <summary>
    /// Unique identifier for the user.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// User's first name.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User's last name.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Email address used by the external message service.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's date of birth.
    /// </summary>
    public DateOnly Birthday { get; set; }

    /// <summary>
    /// Optional anniversary date used for anniversary notifications.
    /// </summary>
    public DateOnly? AnniversaryDate { get; set; }

    /// <summary>
    /// Time zone identifier used to schedule local 9:00 AM notification messages.
    /// </summary>
    public string TimeZoneId { get; set; } = string.Empty;

    /// <summary>
    /// Free-text location description supplied for display or auditing.
    /// </summary>
    public string LocationText { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the user has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// UTC timestamp when the user was created.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>
    /// UTC timestamp when the user was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }

    /// <summary>
    /// Notification records owned by the user.
    /// </summary>
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    /// <summary>
    /// User's first and last name combined for message rendering.
    /// </summary>
    public string FullName => $"{FirstName} {LastName}".Trim();
}
