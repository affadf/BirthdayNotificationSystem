using BirthdayNotificationSystem.Application.Interfaces;
using BirthdayNotificationSystem.Application.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BirthdayNotificationSystem.Infrastructure.Services;

/// <summary>
/// Background worker that dispatches due notifications.
/// </summary>
public sealed class NotificationWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<WorkerOptions> options,
    ILogger<NotificationWorker> logger) : BackgroundService
{
    private readonly WorkerOptions _options = options.Value;

    /// <summary>
    /// Runs the notification process on each configured polling interval.
    ///
    /// Notification statuses are used as follows:
    /// - Pending: the message is waiting to be sent.
    /// - Processing: the message is currently being handled.
    /// - Sent: the message was delivered.
    /// - Failed: the message could not be delivered and can be retried.
    /// - Canceled: the message should no longer be sent.
    ///
    /// Each cycle recovers stuck notifications by changing expired Processing records to Failed,
    /// then sends due Pending or Failed notifications by moving them to Processing first.
    /// A successful message changes from Processing to Sent, a delivery problem changes it to
    /// Failed for retry, and a deleted or missing user changes it to Canceled.
    ///
    /// After a successful send, the next year's notification of the same type is created as
    /// Pending. The cycle also creates missing upcoming Pending notifications for active users.
    /// </summary>
    /// <param name="stoppingToken">Signals that the host is shutting down.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Notification worker is disabled.");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.PollingIntervalSeconds));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var dispatcher = scope.ServiceProvider.GetRequiredService<INotificationDispatcher>();
                    var recovery = scope.ServiceProvider.GetRequiredService<INotificationRecoveryService>();

                    var recovered = await recovery.RecoverExpiredLocksAsync(stoppingToken);
                    var sent = await dispatcher.DispatchDueNotificationsAsync(stoppingToken);

                    var created = await recovery.EnsureUpcomingNotificationSchedulesAsync(stoppingToken);

                    if (created > 0)
                    {
                        logger.LogInformation("Created {Created} missing notification schedules.", created);
                    }

                    if (recovered > 0 || sent > 0 || created > 0)
                    {
                        logger.LogInformation(
                            "Notification worker recovered {Recovered}, sent {Sent}, and created {Created} schedules.",
                            recovered,
                            sent,
                            created);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Notification worker iteration failed.");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
    }
}
