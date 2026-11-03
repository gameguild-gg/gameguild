using GameGuild.Identity.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Periodically prunes expired and long-revoked trusted-device rows (issue #1144). Without
///     this sweep the trusted-device table grows without bound: expired trust rows stay readable
///     forever and revoked rows are never dropped. The sweep only deletes rows the repository
///     already considers dead (expired, or inactive for over 90 days), so it can never remove an
///     operative trust.
/// </summary>
public sealed class TrustedDeviceCleanupBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<TrustedDeviceCleanupBackgroundService> logger) : BackgroundService
{
    /// <summary>Delay before the first sweep after startup.</summary>
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(15);

    /// <summary>Cadence between sweeps; pruning is not latency-sensitive.</summary>
    private static readonly TimeSpan SweepInterval = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
                using var scope = scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<ITrustedDeviceRepository>();
                await repository.DeleteExpiredAsync(SystemClock.UtcNow, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                // A failed sweep retries on the next interval; trust decisions never depend on it.
                logger.LogWarning(exception, "Trusted-device pruning failed; retrying on the next interval.");
            }

            try
            {
                await Task.Delay(SweepInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }
}
