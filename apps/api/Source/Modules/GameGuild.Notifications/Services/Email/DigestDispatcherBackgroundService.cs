using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Notifications.Services.Email;

/// <summary>
/// Host wrapper for <see cref="DigestDispatcherService"/>: scope-per-pass loop honoring Interval,
/// logging exceptions without crashing, first pass delayed so startup is never blocked.
/// </summary>
public sealed class DigestDispatcherBackgroundService(
    IServiceProvider serviceProvider,
    IOptions<DigestDispatcherOptions> options,
    ILogger<DigestDispatcherBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("DigestDispatcherBackgroundService started");

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
                var dispatcher = scope.ServiceProvider.GetRequiredService<DigestDispatcherService>();
                var digestsSent = await dispatcher.SweepOnceAsync(stoppingToken).ConfigureAwait(false);
                if (digestsSent > 0)
                {
                    logger.LogInformation("Digest dispatcher sweep sent {Count} digest email(s)", digestsSent);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Digest dispatcher sweep failed; will retry on next tick");
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
