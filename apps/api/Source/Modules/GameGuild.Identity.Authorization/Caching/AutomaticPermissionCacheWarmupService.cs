using GameGuild.Configuration.PresentationLayer.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Identity.Authorization.Caching;

/// <summary>Periodically warms the most frequently accessed subject-based ACL decisions from the previous window.</summary>
public sealed class AutomaticPermissionCacheWarmupService(
    IServiceScopeFactory scopeFactory,
    IPermissionCachePopularityTracker popularityTracker,
    IOptions<AuthorizationCacheOptions> options,
    ILogger<AutomaticPermissionCacheWarmupService> logger) : BackgroundService
{
    private readonly AuthorizationCacheOptions _options = options.Value;

    /// <summary>Runs one bounded popularity-based warmup cycle. Exposed for deterministic integration testing.</summary>
    public async Task<PermissionCacheWarmupResult> RunCycleAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.AutomaticWarmupEnabled)
        {
            return new PermissionCacheWarmupResult(0, 0, 0);
        }

        var candidates = popularityTracker.Drain(
            _options.AutomaticWarmupMinimumAccessCount,
            _options.AutomaticWarmupMaxEntriesPerCycle);
        if (candidates.Count == 0)
        {
            return new PermissionCacheWarmupResult(0, 0, 0);
        }

        using var suppression = popularityTracker.SuppressTracking();
        await using var scope = scopeFactory.CreateAsyncScope();
        var warmupService = scope.ServiceProvider.GetRequiredService<IPermissionCacheWarmupService>();
        var requests = candidates.Select(candidate => candidate.Request).ToArray();
        var result = await warmupService.WarmAsync(requests, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Automatically warmed {WarmedCount} of {CandidateCount} popular permission-cache entries.",
            result.Warmed,
            candidates.Count);
        return result;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _options.Validate();
        if (!_options.AutomaticWarmupEnabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.AutomaticWarmupIntervalSeconds));
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                await RunCycleAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                // A failed warmup must never stop authorization or the next scheduled cycle.
                logger.LogWarning(exception, "Automatic permission-cache warmup failed; a later cycle will retry.");
            }
        }
    }
}
