using FluentAssertions;
using GameGuild.Configuration.PresentationLayer.Authorization;
using GameGuild.Identity.Authorization.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests;

public sealed class PermissionCacheMemoryPressureTests
{
    [Fact]
    public void HighPressureShrinksTrackedL1AndPreservesRecentAndUnrelatedEntries()
    {
        using var harness = new Harness();
        harness.Cache.Set("unrelated", new object());
        harness.Tracker.Touch("entry-0");

        var report = harness.Sample(90, 100);

        report.UnderPressure.Should().BeTrue();
        report.EffectiveCapacity.Should().Be(6);
        report.EntriesEvicted.Should().Be(6);
        harness.Tracker.GetStatistics().TotalEntries.Should().Be(6);
        harness.Cache.TryGetValue("unrelated", out _).Should().BeTrue();
        harness.Cache.TryGetValue("entry-0", out _).Should().BeTrue();
        foreach (var index in Enumerable.Range(1, 6))
        {
            harness.Cache.TryGetValue($"entry-{index}", out _).Should().BeFalse();
        }
        foreach (var index in Enumerable.Range(7, 5))
        {
            harness.Cache.TryGetValue($"entry-{index}", out _).Should().BeTrue();
        }
        harness.Metrics.Verify(metrics => metrics.RecordEviction(CacheLevel.L1, "acl", "memory_pressure"), Times.Exactly(6));
    }

    [Fact]
    public void SustainedPressureRespectsMinimumAndRecoveryRestoresCapacityGradually()
    {
        using var harness = new Harness();
        harness.Sample(90, 100).EffectiveCapacity.Should().Be(6);
        harness.Sample(90, 100).EffectiveCapacity.Should().Be(3);
        harness.Sample(90, 100).EffectiveCapacity.Should().Be(3);
        harness.Sample(80, 100).EffectiveCapacity.Should().Be(3, "the hysteresis band must hold the reduced budget");
        harness.Sample(60, 100).EffectiveCapacity.Should().Be(6);
        harness.Sample(60, 100).EffectiveCapacity.Should().Be(9);
        harness.Sample(60, 100).EffectiveCapacity.Should().Be(12);
        harness.Sample(60, 100).EffectiveCapacity.Should().Be(12);
        harness.Tracker.GetStatistics().TotalEntries.Should().Be(3, "restoring capacity does not manufacture cached decisions");
    }

    [Theory]
    [InlineData(84, 12)]
    [InlineData(85, 6)]
    [InlineData(100, 6)]
    public void HighPressureThresholdIsInclusive(long load, int expectedCapacity)
    {
        using var harness = new Harness();
        harness.Sample(load, 100).EffectiveCapacity.Should().Be(expectedCapacity);
    }

    [Theory]
    [InlineData(70, 9)]
    [InlineData(71, 6)]
    public void RecoveryThresholdIsInclusive(long load, int expectedCapacity)
    {
        using var harness = new Harness();
        harness.Sample(90, 100);
        harness.Sample(load, 100).EffectiveCapacity.Should().Be(expectedCapacity);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(-1, 100)]
    public void UnknownMemorySnapshotPreservesTheExistingBudget(long load, long threshold)
    {
        using var harness = new Harness();
        harness.Sample(90, 100);
        var report = harness.Sample(load, threshold);
        report.SnapshotValid.Should().BeFalse();
        report.EffectiveCapacity.Should().Be(6);
        report.EntriesEvicted.Should().Be(0);
    }

    [Fact]
    public void DisabledAdaptationDoesNotReadTheRuntimeSignalOrResize()
    {
        using var harness = new Harness(new AuthorizationCacheOptions { EnableL1MemoryPressureAdaptation = false });
        var report = harness.Monitor.ReportSnapshot();
        report.Enabled.Should().BeFalse();
        harness.Tracker.EffectiveCapacity.Should().Be(12);
        harness.Signal.Verify(signal => signal.GetSnapshot(), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(13)]
    public void CapacityManagerRejectsValuesOutsideItsConfiguredLimit(int capacity)
    {
        using var harness = new Harness();
        var resize = () => harness.Tracker.ResizeCapacity(capacity, "memory_pressure");
        resize.Should().Throw<ArgumentOutOfRangeException>();
        harness.Tracker.EffectiveCapacity.Should().Be(12);
    }

    [Fact]
    public void NewEntriesRespectTheReducedBudgetAndRetainTheNewDecision()
    {
        using var harness = new Harness();
        harness.Sample(90, 100);
        harness.Track("new-decision");
        harness.Tracker.GetStatistics().TotalEntries.Should().Be(6);
        harness.Cache.TryGetValue("new-decision", out _).Should().BeTrue();
        harness.Tracker.EffectiveCapacity.Should().Be(6);
    }

    [Fact]
    public void SingleEntryBudgetNeverShrinksToZero()
    {
        using var harness = new Harness(capacity: 1);
        harness.Sample(100, 100).EffectiveCapacity.Should().Be(1);
        harness.Cache.TryGetValue("entry-0", out _).Should().BeTrue();
    }

    [Fact]
    public void PressureNeverRaisesAnAlreadySmallerBudget()
    {
        using var harness = new Harness();
        harness.Tracker.ResizeCapacity(2, "capacity");
        harness.Sample(90, 100).EffectiveCapacity.Should().Be(2);
    }

    [Fact]
    public async Task BackgroundSamplingFailureDoesNotStopTheHostAndShutdownCancelsTheTimer()
    {
        using var harness = new Harness();
        var observed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        harness.Signal.Setup(signal => signal.GetSnapshot())
            .Callback(() => observed.TrySetResult())
            .Throws(new InvalidOperationException("Memory signal unavailable"));
        await harness.Monitor.StartAsync(CancellationToken.None);
        try
        {
            await observed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
        finally
        {
            await harness.Monitor.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10));
        }
        harness.Tracker.EffectiveCapacity.Should().Be(12);
    }

    [Fact]
    public async Task DisabledBackgroundServiceDoesNotSample()
    {
        using var harness = new Harness(new AuthorizationCacheOptions { EnableL1MemoryPressureAdaptation = false });
        await harness.Monitor.StartAsync(CancellationToken.None);
        await harness.Monitor.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10));
        harness.Signal.Verify(signal => signal.GetSnapshot(), Times.Never);
    }

    [Fact]
    public void RegistrationSharesTheTrackerAndCapacityManagerAcrossScopes()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddAuthorizationCaching(options => options.MaxL1CacheSize = 12);
        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();
        var tracker = provider.GetRequiredService<IPermissionCacheKeyTracker>();
        var capacity = scope.ServiceProvider.GetRequiredService<IPermissionCacheCapacityManager>();
        capacity.Should().BeSameAs(tracker);
        capacity.ConfiguredCapacity.Should().Be(12);
        provider.GetServices<IHostedService>().Should().ContainSingle(service => service is PermissionCacheMemoryPressureMonitor);
    }

    [Theory]
    [InlineData("interval", 0)]
    [InlineData("high", 0)]
    [InlineData("high", 101)]
    [InlineData("recovery", -1)]
    [InlineData("recovery", 85)]
    [InlineData("minimum", 0)]
    [InlineData("minimum", 101)]
    [InlineData("reduction", 0)]
    [InlineData("reduction", 101)]
    [InlineData("step", 0)]
    [InlineData("step", 101)]
    public void InvalidPressureConfigurationIsRejected(string setting, int value)
    {
        var options = new AuthorizationCacheOptions();
        switch (setting)
        {
            case "interval": options.L1MemoryPressureCheckIntervalSeconds = value; break;
            case "high": options.L1MemoryPressureHighThresholdPercent = value; break;
            case "recovery": options.L1MemoryPressureRecoveryThresholdPercent = value; break;
            case "minimum": options.L1MemoryPressureMinimumCapacityPercent = value; break;
            case "reduction": options.L1MemoryPressureReductionPercent = value; break;
            case "step": options.L1MemoryPressureRecoveryStepPercent = value; break;
            default: throw new ArgumentOutOfRangeException(nameof(setting));
        }
        var validate = () => options.Validate();
        validate.Should().Throw<InvalidOperationException>();
    }

    private sealed class Harness : IDisposable
    {
        public MemoryCache Cache { get; } = new(new MemoryCacheOptions());
        public Mock<ICacheMetricsService> Metrics { get; } = new();
        public Mock<IPermissionCacheMemoryPressureProvider> Signal { get; } = new();
        public PermissionCacheKeyTracker Tracker { get; }
        public PermissionCacheMemoryPressureMonitor Monitor { get; }

        public Harness(AuthorizationCacheOptions? options = null, int capacity = 12)
        {
            Tracker = new PermissionCacheKeyTracker(Cache, Metrics.Object, capacity);
            Monitor = new PermissionCacheMemoryPressureMonitor(Tracker, Signal.Object,
                Options.Create(options ?? new AuthorizationCacheOptions()), NullLogger<PermissionCacheMemoryPressureMonitor>.Instance);
            foreach (var index in Enumerable.Range(0, capacity)) { Track($"entry-{index}"); }
        }

        public void Track(string key)
        {
            var options = new MemoryCacheEntryOptions();
            Tracker.Track(key, "acl", options);
            Cache.Set(key, new object(), options);
        }

        public PermissionCacheMemoryPressureReport Sample(long load, long threshold)
        {
            Signal.Setup(signal => signal.GetSnapshot()).Returns(new PermissionCacheMemorySnapshot(load, threshold));
            return Monitor.ReportSnapshot();
        }

        public void Dispose()
        {
            Monitor.Dispose();
            Cache.Dispose();
        }
    }
}
