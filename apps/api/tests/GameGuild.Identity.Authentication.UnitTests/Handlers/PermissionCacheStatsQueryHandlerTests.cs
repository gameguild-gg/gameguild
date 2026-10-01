using System.Reflection;
using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Authorization.Caching;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Handlers;

public sealed class PermissionCacheStatsQueryHandlerTests
{
    [Fact]
    public async Task Handle_MapsMetricsAndTrackedL1EntriesToStatisticsDto()
    {
        var tenantId = Guid.NewGuid();
        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();
        var metrics = new Mock<ICacheMetricsService>();
        metrics.Setup(service => service.GetStatistics()).Returns(new CacheStatistics
        {
            L1Hits = 8,
            L2Hits = 2,
            Misses = 2,
            Evictions = 1,
            ByType = new Dictionary<string, CacheTypeStatistics>(StringComparer.Ordinal)
            {
                ["permission"] = new() { CacheType = "permission", L1Hits = 8, L2Hits = 2, Misses = 2 }
            },
            LookupDurationByType = new Dictionary<string, CacheLookupStatistics>(StringComparer.Ordinal)
            {
                ["acl"] = new(4, 3.5)
            }
        });

        var tracker = new PermissionCacheKeyTracker(
            new MemoryCache(new MemoryCacheOptions()),
            metrics.Object);
        tracker.Track($"perm:{tenantId}:{firstUserId}:courses:read", "permission");
        tracker.Track($"acl:{tenantId}:{firstUserId}:Document:doc-1:tv0:uv0:gv0", "acl");
        tracker.Track($"acl:subj:{tenantId}:{secondUserId}:role-id:group-id:Document:doc-2:tv0:uv0:gv0", "acl");
        tracker.Track($"policy:course-editor|{tenantId}|v0", "policy");

        var handler = new GetPermissionCacheStatsQueryHandler(metrics.Object, tracker);
        var before = DateTime.UtcNow;
        var result = await handler.Handle(new GetPermissionCacheStatsQuery(), CancellationToken.None);
        var after = DateTime.UtcNow;

        result.TotalCachedUsers.Should().Be(2);
        result.TotalCachedPermissions.Should().Be(1);
        result.TotalCachedAclEntries.Should().Be(2);
        result.TotalCachedPolicies.Should().Be(1);
        result.CacheSize.Should().Be(4);
        result.L1Hits.Should().Be(8);
        result.L2Hits.Should().Be(2);
        result.CacheMisses.Should().Be(2);
        result.CacheEvictions.Should().Be(1);
        result.TotalRequests.Should().Be(12);
        result.CacheHitRate.Should().BeApproximately(10d / 12d, 0.0001);
        result.ByType.Should().ContainKey("permission");
        result.PerformanceMetrics.Should().ContainSingle().Which.Should().Match<CachePerformanceMetric>(metric =>
            metric.Operation == "acl" &&
            metric.RequestCount == 4 &&
            Math.Abs(metric.AverageTime - 3.5) < 0.0001);
        result.PerformanceMetrics[0].Timestamp.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        result.LastUpdated.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void AddAuthenticationApplication_RegistersCacheStatisticsQueryHandler()
    {
        var services = new ServiceCollection();

        services.AddAuthenticationApplication();

        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IQueryHandler<GetPermissionCacheStatsQuery, PermissionCacheStatsDto>) &&
            descriptor.ImplementationType == typeof(GetPermissionCacheStatsQueryHandler));
    }

    [Fact]
    public void GetCacheStatistics_RequiresSystemAdministratorPolicy()
    {
        var method = typeof(PermissionAdminController).GetMethod(nameof(PermissionAdminController.GetCacheStatistics));

        method.Should().NotBeNull();
        method!.GetCustomAttribute<AuthorizeAttribute>()?.Policy.Should().Be(Policies.SystemAdmin);
    }
}
