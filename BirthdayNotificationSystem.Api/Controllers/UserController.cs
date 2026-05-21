using BirthdayNotificationSystem.Application.Contracts;
using BirthdayNotificationSystem.Application.Exceptions;
using BirthdayNotificationSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BirthdayNotificationSystem.Api.Controllers;

/// <summary>
/// Exposes API endpoints for creating, updating, and deleting users in the notification system.
/// </summary>
[ApiController]
[Route("user")]
public sealed class UserController(IUserService userService) : ControllerBase
{
    /// <summary>
    /// Creates a new user and schedules applicable upcoming notifications at 9:00 AM in their local time zone.
    /// </summary>
    /// <param name="request">The user details, birthday, email address, location text, and IANA time zone ID.</param>
    /// <param name="cancellationToken">Stops the operation if the HTTP request is aborted.</param>
    /// <returns>The created user with upcoming notification schedules.</returns>
    [HttpPost]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await userService.CreateAsync(request, cancellationToken);
            return Created($"/user/{user.Id}", user);
        }
        catch (UserInputException ex)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>(ex.Errors)));
        }
    }

    /// <summary>
    /// Updates an active user's details and recalculates future notification schedules.
    /// </summary>
    /// <param name="id">The unique identifier of the user to update.</param>
    /// <param name="request">The replacement user details, birthday, email address, location text, and IANA time zone ID.</param>
    /// <param name="cancellationToken">Stops the operation if the HTTP request is aborted.</param>
    /// <returns>The updated user, or 404 if the user does not exist or was deleted.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await userService.UpdateAsync(id, request, cancellationToken);
            return user is null ? NotFound() : Ok(user);
        }
        catch (UserInputException ex)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>(ex.Errors)));
        }
    }

    /// <summary>
    /// Soft-deletes a user and cancels their future pending notifications.
    /// </summary>
    /// <param name="id">The unique identifier of the user to delete.</param>
    /// <param name="cancellationToken">Stops the operation if the HTTP request is aborted.</param>
    /// <returns>No content when deletion succeeds, or 404 if the user does not exist or was already deleted.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await userService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>
    /// Soft-deletes a user by query string ID and cancels their future pending notifications.
    /// </summary>
    /// <param name="id">The unique identifier of the user to delete.</param>
    /// <param name="cancellationToken">Stops the operation if the HTTP request is aborted.</param>
    /// <returns>No content when deletion succeeds, or 404 if the user does not exist or was already deleted.</returns>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteByQuery([FromQuery] Guid id, CancellationToken cancellationToken)
    {
        var deleted = await userService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
