using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Persists expired rotation revocations without waiting for another credential use.
///     Authentication continues to reject expired overlap windows immediately.
/// </summary>
public sealed class ApiKeyRotationRevocationBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<ApiKeyRotationRevocationBackgroundService> logger,
    TimeProvider? timeProvider = null) : BackgroundService
{
    internal const int BatchSize = 128;
    internal static readonly TimeSpan SweepInterval = TimeSpan.FromMinutes(1);

    internal async Task<int> SweepOnceAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IApiKeyRepository>();
        return await repository.FinalizeExpiredRotationRevocationsAsync(BatchSize, cancellationToken).ConfigureAwait(false);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(SweepInterval, timeProvider ?? TimeProvider.System);
        try
        {
            do
            {
                try
                {
                    await SweepOnceAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "API-key rotation revocation failed; retrying on the next interval.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host shutdown also cancels the timer wait.
        }
    }
}
