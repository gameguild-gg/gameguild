using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using FluentAssertions;
using GameGuild.Configuration.PresentationLayer.Authorization;
using GameGuild.Identity.Authorization.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GameGuild.Identity.Authorization.UnitTests;

public sealed class PermissionCacheLatencyMetricTests
{
    [Fact]
    public async Task CacheLookup_EmitsLatencyHistogramWithCacheType()
    {
        const string cacheType = "latency-metric-test";
        var measurements = new ConcurrentQueue<double>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == "GameGuild.Identity.Authorization.Cache" &&
                instrument.Name == "authorization_cache_lookup_duration")
            {
                instrument.Unit.Should().Be("ms");
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) =>
        {
            if (instrument.Name != "authorization_cache_lookup_duration")
                return;

            var hasExpectedCacheType = false;
            foreach (var tag in tags)
            {
                if (tag.Key == "cache_type" && tag.Value?.ToString() == cacheType)
                    hasExpectedCacheType = true;
            }

            if (hasExpectedCacheType)
                measurements.Enqueue(measurement);
        });
        listener.Start();

        using var memoryCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 });
        var metrics = new CacheMetricsService();
        var cache = new HybridPermissionCache(
            memoryCache,
            Options.Create(new AuthorizationCacheOptions { UseDistributedCache = false }),
            metrics,
            NullLogger<HybridPermissionCache>.Instance);
        await cache.SetValueAsync("acl:metric-key", true, cacheType);

        var result = await cache.GetValueAsync<bool>("acl:metric-key", cacheType);

        result.Found.Should().BeTrue();
        measurements.Should().ContainSingle().Which.Should().BeGreaterThanOrEqualTo(0);
    }
}
