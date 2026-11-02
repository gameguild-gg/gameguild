using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Background maintenance for the trusted-device registry (issue #1144): abandoned trust
///     rows are pruned on a schedule so the table does not grow unbounded. Reuses the
///     repository's existing <c>DeleteExpiredAsync</c> cleanup, which removes rows that have
///     been expired — or revoked/deactivated — for more than 90 days. The retention grace
///     matters: a merely-expired row is still renewed in place when its device is re-trusted
///     (<see cref="SessionManagementService.TrustDeviceAsync"/> keeps the row id and trust
///     history), so a sweep must never race that renewal.
/// </summary>
public sealed class TrustedDeviceMaintenanceBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<TrustedDeviceMaintenanceBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan SweepInterval = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Trusted-device maintenance background service started");

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
                var trustedDeviceRepository = scope.ServiceProvider.GetRequiredService<ITrustedDeviceRepository>();
                await trustedDeviceRepository.DeleteExpiredAsync(SystemClock.UtcNow, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // A failed sweep must never crash the host; the next tick retries.
                logger.LogError(ex, "Trusted-device expired-row cleanup failed; will retry on next sweep");
            }

            try
            {
                await Task.Delay(SweepInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        logger.LogInformation("Trusted-device maintenance background service stopped");
    }
}
