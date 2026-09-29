using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using FluentAssertions;
using GameGuild.Configuration.PresentationLayer.Authorization;
using GameGuild.Identity.Authorization.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace GameGuild.Identity.Authorization.UnitTests;

public sealed class PermissionCacheLatencyMetricTests
{
    [Fact]
    public void RecordLookupDuration_ExposesBoundedAverageAndCountByCacheType()
    {
        var metrics = new CacheMetricsService();

        metrics.RecordLookupDuration(TimeSpan.FromMilliseconds(2), "acl");
        metrics.RecordLookupDuration(TimeSpan.FromMilliseconds(6), "acl");
        metrics.RecordLookupDuration(TimeSpan.FromMilliseconds(9), "permission");

        var statistics = metrics.GetStatistics();

        statistics.LookupDurationByType.Should().ContainKey("acl");
        statistics.LookupDurationByType["acl"].Count.Should().Be(2);
        statistics.LookupDurationByType["acl"].AverageMilliseconds.Should().BeApproximately(4, 0.0001);
        statistics.LookupDurationByType["permission"].Count.Should().Be(1);
        statistics.LookupDurationByType["permission"].AverageMilliseconds.Should().BeApproximately(9, 0.0001);
    }

    [Fact]
    public async Task CachedAclL1Hit_RecordsLookupDuration()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantVersions = new Mock<ITenantSecurityVersionStore>();
        tenantVersions
            .Setup(store => store.GetTenantAndGlobalVersionsAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((1L, 1L));
        var userVersions = new Mock<IUserSecurityVersionStore>();
        userVersions.Setup(store => store.GetVersionAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(1L);
        var innerService = new Mock<IAccessControlListService>();
        innerService
            .Setup(service => service.EvaluateAccessAsync(
                It.IsAny<AclSubject>(),
                tenantId,
                "Document",
                "latency-document",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(AccessLevel.Write);

        using var memoryCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 });
        var metrics = new CacheMetricsService();
        var service = new CachedAccessControlListService(
            innerService.Object,
            memoryCache,
            tenantVersions.Object,
            userVersions.Object,
            Options.Create(new AuthorizationCacheOptions { AccessControlListTtlSeconds = 60 }),
            hybridCache: null,
            metrics);
        var subject = AclSubject.ForUser(userId);

        await service.EvaluateAccessAsync(subject, tenantId, "Document", "latency-document");
        await service.EvaluateAccessAsync(subject, tenantId, "Document", "latency-document");

        innerService.Verify(
            inner => inner.EvaluateAccessAsync(
                It.IsAny<AclSubject>(),
                tenantId,
                "Document",
                "latency-document",
                It.IsAny<CancellationToken>()),
            Times.Once);
        metrics.GetStatistics().LookupDurationByType["acl"].Count.Should().Be(2);
    }

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
            {
                return;
            }

            var hasExpectedCacheType = false;
            foreach (var tag in tags)
            {
                if (tag.Key == "cache_type" && tag.Value?.ToString() == cacheType)
                {
                    hasExpectedCacheType = true;
                }
            }

            if (hasExpectedCacheType)
            {
                measurements.Enqueue(measurement);
            }
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

    [Fact]
    public async Task BulkCacheLookup_EmitsOneLatencyMeasurementPerDistinctKey()
    {
        const string cacheType = "bulk-latency-metric-test";
        var measurements = new ConcurrentQueue<double>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == "GameGuild.Identity.Authorization.Cache" &&
                instrument.Name == "authorization_cache_lookup_duration")
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<double>((instrument, _, tags, _) =>
        {
            if (instrument.Name != "authorization_cache_lookup_duration")
            {
                return;
            }

            foreach (var tag in tags)
            {
                if (tag.Key == "cache_type" && tag.Value?.ToString() == cacheType)
                {
                    measurements.Enqueue(0);
                    break;
                }
            }
        });
        listener.Start();

        using var memoryCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 });
        var cache = new HybridPermissionCache(
            memoryCache,
            Options.Create(new AuthorizationCacheOptions { UseDistributedCache = false }),
            new CacheMetricsService(),
            NullLogger<HybridPermissionCache>.Instance);
        await cache.SetValueAsync("acl:bulk-cached", true, cacheType);

        var result = await cache.GetManyValuesAsync<bool>(["acl:bulk-cached", "acl:bulk-missing"], cacheType);

        result["acl:bulk-cached"].Should().Be(CacheResult<bool>.Hit(true));
        result["acl:bulk-missing"].Found.Should().BeFalse();
        measurements.Should().HaveCount(2);
    }
}
