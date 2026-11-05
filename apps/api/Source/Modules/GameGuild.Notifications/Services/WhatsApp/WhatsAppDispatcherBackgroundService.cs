using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Notifications.Services.WhatsApp;

/// <summary>
///     Host wrapper for <see cref="WhatsAppDispatcherService"/> (issue #54): scope-per-pass
///     loop honoring Interval, logging exceptions without crashing, first sweep delayed so
///     startup is never blocked. Exits immediately while the channel is unconfigured — a
///     disabled channel must not poll the database (issue #1218 G6).
/// </summary>
public sealed class WhatsAppDispatcherBackgroundService(
    IServiceProvider serviceProvider,
    IOptions<WhatsAppDispatcherOptions> options,
    IOptions<WhatsAppSettings> settings,
    ILogger<WhatsAppDispatcherBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!settings.Value.Enabled)
        {
            logger.LogInformation("WhatsAppDispatcherBackgroundService is dormant because WhatsApp delivery is disabled (no provider configured)");
            return;
        }

        logger.LogInformation("WhatsAppDispatcherBackgroundService started");

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
                var dispatcher = scope.ServiceProvider.GetRequiredService<WhatsAppDispatcherService>();
                var processed = await dispatcher.SweepOnceAsync(stoppingToken).ConfigureAwait(false);
                if (processed > 0)
                {
                    logger.LogInformation("WhatsApp dispatcher sweep processed {Count} notification(s)", processed);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "WhatsApp dispatcher sweep failed; will retry on next tick");
            }

            try
            {
                await Task.Delay(options.Value.Interval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
