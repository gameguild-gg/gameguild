using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Entities;

public class TenantStatisticsTests
{
    [Fact]
    public void UpdateMemberStats_Should_Update_Counts()
    {
        var stats = new TenantStatistics();

        stats.UpdateMemberStats(total: 10, active: 7, inactive: 3);

        stats.TotalMembers.Should().Be(10);
        stats.ActiveMembers.Should().Be(7);
        stats.InactiveMembers.Should().Be(3);
    }

    [Fact]
    public void UpdateStorageUsage_Should_Update_Storage()
    {
        var stats = new TenantStatistics();

        stats.UpdateStorageUsage(1024);

        stats.StorageUsed.Should().Be(1024);
    }

    [Fact]
    public void IncrementApiCalls_Should_Add_Count()
    {
        var stats = new TenantStatistics { ApiCalls = 5 };

        stats.IncrementApiCalls();
        stats.IncrementApiCalls(3);

        stats.ApiCalls.Should().Be(9);
    }
}
