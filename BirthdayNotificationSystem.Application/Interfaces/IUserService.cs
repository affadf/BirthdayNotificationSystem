using BirthdayNotificationSystem.Application.Contracts;

namespace BirthdayNotificationSystem.Application.Interfaces;

/// <summary>
/// Defines user workflows that also keep notification schedules in sync.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Creates a user and schedules applicable upcoming notifications.
    /// </summary>
    /// <param name="request">The user details to create.</param>
    /// <param name="cancellationToken">Stops the operation if the request is canceled.</param>
    /// <returns>The created user and their upcoming notification schedules.</returns>
    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Updates a user and recalculates their pending future notifications.
    /// </summary>
    /// <param name="id">The user identifier to update.</param>
    /// <param name="request">The replacement user details.</param>
    /// <param name="cancellationToken">Stops the operation if the request is canceled.</param>
    /// <returns>The updated user, or null when the user is missing or deleted.</returns>
    Task<UserResponse?> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Soft-deletes a user and cancels pending future notifications.
    /// </summary>
    /// <param name="id">The user identifier to delete.</param>
    /// <param name="cancellationToken">Stops the operation if the request is canceled.</param>
    /// <returns>True when a user was deleted; otherwise false.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
