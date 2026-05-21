namespace BirthdayNotificationSystem.Application.Exceptions;

/// <summary>
/// Represents validation errors that are safe to return to API callers.
/// </summary>
public sealed class UserInputException(IReadOnlyDictionary<string, string[]> errors) : Exception("Invalid user input.")
{
    /// <summary>
    /// Gets validation errors keyed by request field name.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
