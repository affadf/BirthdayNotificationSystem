using BirthdayNotificationSystem.Domain;
using Microsoft.EntityFrameworkCore;

namespace BirthdayNotificationSystem.Application.Interfaces;

/// <summary>
/// Provides application services with the EF Core sets they need without referencing the infrastructure project.
/// </summary>
public interface IApplicationDbContext
{
    /// <summary>
    /// Users registered for notifications.
    /// </summary>
    DbSet<User> Users { get; }

    /// <summary>
    /// Durable notification records processed by the worker.
    /// </summary>
    DbSet<Notification> Notifications { get; }

    /// <summary>
    /// Saves pending changes to the database.
    /// </summary>
    /// <param name="cancellationToken">Stops the operation if the request is canceled.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
