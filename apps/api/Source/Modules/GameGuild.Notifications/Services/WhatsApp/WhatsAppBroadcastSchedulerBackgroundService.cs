using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Notifications.Services.WhatsApp;

/// <summary>
///     Host wrapper that executes due scheduled broadcasts (issue #55): sweeps
///     Scheduled campaigns whose time has come, claims them, and delegates to
///     <see cref="IWhatsAppBroadcastService.SendAsync"/>. Single-flight by the
///     campaign state machine (Draft/Scheduled -> Sending) — a second claim attempt on
///     an in-flight campaign fails benignly. Dormant while the channel is unconfigured
///     because no broadcasts can be sent in that state.
/// </summary>
public sealed class WhatsAppBroadcastSchedulerBackgroundService(
    IServiceProvider serviceProvider,
    IOptions<WhatsAppBroadcastOptions> options,
    IOptions<WhatsAppSettings> settings,
    ILogger<WhatsAppBroadcastSchedulerBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!settings.Value.Enabled)
        {
            logger.LogInformation("WhatsAppBroadcastSchedulerBackgroundService is dormant because WhatsApp delivery is disabled");
        }

        logger.LogInformation("WhatsAppBroadcastSchedulerBackgroundService started");

        try
        {
            await Task.Delay(InitialDelay, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var broadcastService = scope.ServiceProvider.GetRequiredService<IWhatsAppBroadcastService>();

                var due = await context.Set<WhatsAppBroadcast>()
                    .Where(b => b.Status == WhatsAppBroadcastStatus.Scheduled
                        && b.ScheduledAtUtc <= SystemClock.UtcNow)
                    .OrderBy(b => b.ScheduledAtUtc)
                    .Take(10)
                    .ToListAsync(stoppingToken)
                    .ConfigureAwait(false);

                foreach (var broadcast in due)
                {
                    var report = await broadcastService
                        .SendAsync(broadcast.TenantId!.Value, broadcast.Id, stoppingToken)
                        .ConfigureAwait(false);

                    if (report.IsSuccess)
                    {
                        logger.LogInformation(
                            "Scheduled WhatsApp broadcast delivered. BroadcastId: {BroadcastId}, Sent: {Sent}, Failed: {Failed}, Skipped: {Skipped}",
                            broadcast.Id, report.Value.Sent, report.Value.Failed, report.Value.Skipped);
                    }
                    else
                    {
                        logger.LogWarning(
                            "Scheduled WhatsApp broadcast failed. BroadcastId: {BroadcastId}, Error: {Error}",
                            broadcast.Id, report.Error.Description);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "WhatsApp broadcast scheduler sweep failed; will retry on next tick");
            }

            try
            {
                await Task.Delay(options.Value.SchedulerInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
