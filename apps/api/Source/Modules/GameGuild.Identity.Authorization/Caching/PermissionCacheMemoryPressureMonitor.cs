using GameGuild.Configuration.PresentationLayer.Authorization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Identity.Authorization.Caching;

/// <summary>Controls only the tracked authorization L1 entry budget.</summary>
public interface IPermissionCacheCapacityManager
{
    /// <summary>Gets the startup-configured maximum entry count.</summary>
    int ConfiguredCapacity { get; }

    /// <summary>Gets the currently enforced tracked-entry budget.</summary>
    int EffectiveCapacity { get; }

    /// <summary>Changes the budget within the configured maximum and evicts excess tracked entries.</summary>
    int ResizeCapacity(int capacity, string reason);
}

/// <summary>The runtime GC's most recent memory-load signal, in bytes.</summary>
public readonly record struct PermissionCacheMemorySnapshot(long MemoryLoadBytes, long HighMemoryLoadThresholdBytes);

/// <summary>Provides a memory signal without coupling the cache policy to an operating system.</summary>
public interface IPermissionCacheMemoryPressureProvider
{
    /// <summary>Gets the latest available memory snapshot; a nonpositive threshold is unknown.</summary>
    PermissionCacheMemorySnapshot GetSnapshot();
}

/// <summary>Reads GC metadata without forcing a collection or allocating a pressure workload.</summary>
public sealed class RuntimePermissionCacheMemoryPressureProvider : IPermissionCacheMemoryPressureProvider
{
    /// <inheritdoc />
    public PermissionCacheMemorySnapshot GetSnapshot()
    {
        var memory = GC.GetGCMemoryInfo();
        return new PermissionCacheMemorySnapshot(memory.MemoryLoadBytes, memory.HighMemoryLoadThresholdBytes);
    }
}

/// <summary>A single pressure observation and its effect on the tracked L1 budget.</summary>
public sealed record PermissionCacheMemoryPressureReport(
    bool Enabled, bool SnapshotValid, bool UnderPressure,
    long MemoryLoadBytes, long HighMemoryLoadThresholdBytes,
    int ConfiguredCapacity, int PreviousCapacity, int EffectiveCapacity, int EntriesEvicted);

/// <summary>Reduces the authorization L1 budget under pressure and restores it gradually after recovery.</summary>
public sealed class PermissionCacheMemoryPressureMonitor : BackgroundService
{
    private readonly IPermissionCacheCapacityManager _capacity;
    private readonly IPermissionCacheMemoryPressureProvider _provider;
    private readonly AuthorizationCacheOptions _options;
    private readonly ILogger<PermissionCacheMemoryPressureMonitor> _logger;
    private readonly object _sampleLock = new();
    private bool _underPressure;

    public PermissionCacheMemoryPressureMonitor(
        IPermissionCacheCapacityManager capacity,
        IPermissionCacheMemoryPressureProvider provider,
        IOptions<AuthorizationCacheOptions> options,
        ILogger<PermissionCacheMemoryPressureMonitor> logger)
    {
        ArgumentNullException.ThrowIfNull(capacity);
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _capacity = capacity;
        _provider = provider;
        _options = options.Value;
        _options.ValidateMemoryPressureSettings();
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableL1MemoryPressureAdaptation)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.L1MemoryPressureCheckIntervalSeconds));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                ReportSnapshot();
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                // Optional resource monitoring must not stop authorization when a signal
                // provider fails. Configuration errors are rejected before this loop.
                _logger.LogWarning(exception, "Permission-cache memory-pressure sampling failed; the next interval will retry.");
            }
            if (!await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                return;
            }
        }
    }

    /// <summary>Samples and applies one policy step; also supports deterministic operational verification.</summary>
    public PermissionCacheMemoryPressureReport ReportSnapshot()
    {
        lock (_sampleLock)
        {
            return ApplySnapshot();
        }
    }

    private PermissionCacheMemoryPressureReport ApplySnapshot()
    {
        var configured = _capacity.ConfiguredCapacity;
        var previous = _capacity.EffectiveCapacity;
        if (!_options.EnableL1MemoryPressureAdaptation)
        {
            return new(false, false, false, 0, 0, configured, previous, previous, 0);
        }

        var snapshot = _provider.GetSnapshot();
        if (snapshot.HighMemoryLoadThresholdBytes <= 0 || snapshot.MemoryLoadBytes < 0)
        {
            return new(true, false, _underPressure, snapshot.MemoryLoadBytes,
                snapshot.HighMemoryLoadThresholdBytes, configured, previous, previous, 0);
        }

        var loadPercent = 100d * snapshot.MemoryLoadBytes / snapshot.HighMemoryLoadThresholdBytes;
        var target = previous;
        var wasUnderPressure = _underPressure;
        if (loadPercent >= _options.L1MemoryPressureHighThresholdPercent)
        {
            _underPressure = true;
            var minimum = Math.Max(1, (int)((long)configured * _options.L1MemoryPressureMinimumCapacityPercent / 100));
            var reduced = (int)((long)previous * (100 - _options.L1MemoryPressureReductionPercent) / 100);
            target = Math.Min(previous, Math.Max(minimum, reduced));
        }
        else if (loadPercent <= _options.L1MemoryPressureRecoveryThresholdPercent)
        {
            _underPressure = false;
            var step = Math.Max(1L, ((long)configured * _options.L1MemoryPressureRecoveryStepPercent + 99) / 100);
            target = (int)Math.Min(configured, previous + step);
        }

        var evicted = target == previous ? 0 : _capacity.ResizeCapacity(target, "memory_pressure");
        if (target != previous || wasUnderPressure != _underPressure)
        {
            _logger.LogInformation(
                "Permission-cache memory budget: underPressure={UnderPressure}, loadBytes={MemoryLoadBytes}, " +
                "highThresholdBytes={HighMemoryLoadThresholdBytes}, configuredEntries={ConfiguredEntries}, " +
                "previousEntries={PreviousEntries}, effectiveEntries={EffectiveEntries}, evictedEntries={EvictedEntries}",
                _underPressure, snapshot.MemoryLoadBytes, snapshot.HighMemoryLoadThresholdBytes,
                configured, previous, _capacity.EffectiveCapacity, evicted);
        }

        return new(true, true, _underPressure, snapshot.MemoryLoadBytes,
            snapshot.HighMemoryLoadThresholdBytes, configured, previous, _capacity.EffectiveCapacity, evicted);
    }
}
