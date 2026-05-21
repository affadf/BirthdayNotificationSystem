using BirthdayNotificationSystem.Application.Interfaces;
using BirthdayNotificationSystem.Domain;
using Microsoft.EntityFrameworkCore;

namespace BirthdayNotificationSystem.Infrastructure.Data;

/// <summary>
/// EF Core database context for users and durable notification records.
/// </summary>
public sealed class BirthdayNotificationDbContext(DbContextOptions<BirthdayNotificationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    /// <summary>
    /// Users registered for notifications.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Durable notification records used by the background worker.
    /// </summary>
    public DbSet<Notification> Notifications => Set<Notification>();

    /// <summary>
    /// Configures entity mappings, indexes, relationships, and duplicate-prevention constraints.
    /// </summary>
    /// <param name="modelBuilder">The EF Core model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(user => user.LastName).HasMaxLength(100).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(320).IsRequired();
            entity.Property(user => user.Birthday).HasColumnType("date").IsRequired();
            entity.Property(user => user.AnniversaryDate).HasColumnType("date");
            entity.Property(user => user.TimeZoneId).HasMaxLength(100).IsRequired();
            entity.Property(user => user.LocationText).HasMaxLength(250).IsRequired();
            entity.Property(user => user.CreatedAtUtc).IsRequired();
            entity.Property(user => user.UpdatedAtUtc).IsRequired();
            entity.HasIndex(user => user.Email).IsUnique();
            entity.HasIndex(user => user.IsDeleted);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasKey(notification => notification.Id);
            entity.Property(notification => notification.NotificationType).HasConversion<int>().IsRequired();
            entity.Property(notification => notification.Status).HasConversion<int>().IsRequired();
            entity.Property(notification => notification.LastError).HasMaxLength(2000);
            entity.Property(notification => notification.LockedBy).HasMaxLength(200);
            entity.Property(notification => notification.CreatedAtUtc).IsRequired();

            entity.HasOne(notification => notification.User)
                .WithMany(user => user.Notifications)
                .HasForeignKey(notification => notification.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(notification => new
            {
                notification.Status,
                notification.ScheduledAtUtc,
                notification.NextRetryAtUtc
            });

            entity.HasIndex(notification => notification.UserId);

            entity.HasIndex(notification => new
                {
                    notification.UserId,
                    notification.NotificationType,
                    notification.EventYear
                })
                .IsUnique()
                .HasFilter("[Status] IN (0, 1, 3)");
        });
    }
}
