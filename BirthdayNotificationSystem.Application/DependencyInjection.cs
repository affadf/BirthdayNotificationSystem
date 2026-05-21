using BirthdayNotificationSystem.Application.Interfaces;
using BirthdayNotificationSystem.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BirthdayNotificationSystem.Application;

/// <summary>
/// Registers application-layer services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds services that contain notification and user workflow logic.
    /// </summary>
    /// <param name="services">The service collection to register with.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ITimeZoneService, TimeZoneService>();
        services.AddScoped<IMessageTemplateService, MessageTemplateService>();
        services.AddScoped<INotificationScheduler, NotificationScheduler>();
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
        services.AddScoped<INotificationRecoveryService, NotificationRecoveryService>();
        services.AddScoped<IUserService, UserService>();

        return services;
    }
}
