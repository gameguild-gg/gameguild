using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>Bounds expired challenge retention without changing active receipt decisions.</summary>
public sealed class StepUpChallengeCleanupBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<StepUpChallengeCleanupBackgroundService> logger,
    TimeProvider? timeProvider = null) : BackgroundService
{
    internal const int BatchSize = 128;
    internal static readonly TimeSpan SweepInterval = TimeSpan.FromMinutes(1);
    private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;

    internal async Task<int> SweepOnceAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var scope = scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IStepUpChallengeStore>();
        return await store.PruneExpiredAsync(_time.GetUtcNow(), BatchSize, cancellationToken).ConfigureAwait(false);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(SweepInterval, _time);
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
                    logger.LogWarning(exception, "Step-up challenge cleanup failed; retrying on the next interval.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host shutdown cancels the timer as well as an active SQL sweep.
        }
    }
}
