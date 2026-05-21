using BirthdayNotificationSystem.Application.Interfaces;
using BirthdayNotificationSystem.Application.Options;
using BirthdayNotificationSystem.Infrastructure.Data;
using BirthdayNotificationSystem.Infrastructure.Options;
using BirthdayNotificationSystem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BirthdayNotificationSystem.Infrastructure;

/// <summary>
/// Registers infrastructure services such as EF Core, email delivery, and background processing.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds infrastructure services needed by the API host.
    /// </summary>
    /// <param name="services">The service collection to register with.</param>
    /// <param name="configuration">Application configuration values.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<EmailServiceOptions>(configuration.GetSection(EmailServiceOptions.SectionName));
        services.Configure<WorkerOptions>(configuration.GetSection(WorkerOptions.SectionName));

        services.AddDbContext<BirthdayNotificationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IApplicationDbContext>(serviceProvider =>
            serviceProvider.GetRequiredService<BirthdayNotificationDbContext>());

        services.AddHttpClient<IMessageSender, EmailServiceMessageSender>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<EmailServiceOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds + 5);
        });

        services.AddHostedService<NotificationWorker>();

        return services;
    }
}
