using FluentAssertions;
using GameGuild.Identity.Authorization.Caching;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests;

public sealed class CacheMetricsSnapshotTests
{
    [Fact]
    public void GetStatistics_ReturnsDetachedPerTypeSnapshots()
    {
        var metrics = new CacheMetricsService();
        metrics.RecordHit(CacheLevel.L1, "acl");

        var snapshot = metrics.GetStatistics();
        snapshot.ByType["acl"].L1Hits = 500;

        metrics.GetStatistics().ByType["acl"].L1Hits.Should().Be(1);
    }
}
