using FluentAssertions;
using GameGuild.Configuration.PresentationLayer.Authorization;
using GameGuild.Identity.Authorization.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace GameGuild.Identity.Authorization.UnitTests;

public sealed class PermissionCacheCapacityTests
{
    [Fact]
    public void Tracker_EvictsL1EntriesWhenConfiguredCapacityIsExceeded()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var metrics = new Mock<ICacheMetricsService>();
        var tracker = new PermissionCacheKeyTracker(memoryCache, metrics.Object, maxTrackedEntries: 2);
        var keys = Enumerable.Range(0, 3)
            .Select(index => $"perm:{Guid.NewGuid()}:{Guid.NewGuid()}:permission-{index}")
            .ToArray();

        foreach (var key in keys)
        {
            var options = new MemoryCacheEntryOptions();
            tracker.Track(key, "permission", options);
            memoryCache.Set(key, true, options);
        }

        tracker.GetStatistics().TotalEntries.Should().Be(2);
        keys.Count(key => memoryCache.TryGetValue(key, out _)).Should().Be(2);
        metrics.Verify(
            service => service.RecordEviction(CacheLevel.L1, "permission", "capacity"),
            Times.Once);
    }

    [Fact]
    public void Tracker_RejectsNonPositiveCapacity()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());

        var act = () => new PermissionCacheKeyTracker(memoryCache, Mock.Of<ICacheMetricsService>(), maxTrackedEntries: 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AddAuthorizationCaching_UsesConfiguredL1Capacity()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.Configure<AuthorizationCacheOptions>(options => options.MaxL1CacheSize = 1);
        services.AddAuthorizationCaching();
        using var provider = services.BuildServiceProvider();
        var tracker = provider.GetRequiredService<IPermissionCacheKeyTracker>();
        var memoryCache = provider.GetRequiredService<IMemoryCache>();

        foreach (var index in Enumerable.Range(0, 2))
        {
            var key = $"permission-cache-{index}";
            var options = new MemoryCacheEntryOptions();
            tracker.Track(key, "permission", options);
            memoryCache.Set(key, true, options);
        }

        tracker.GetStatistics().TotalEntries.Should().Be(1);
    }
}
