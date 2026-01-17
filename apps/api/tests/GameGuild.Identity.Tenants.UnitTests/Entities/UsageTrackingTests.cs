using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Entities;

public class UsageTrackingTests
{
    [Fact]
    public void UpdateUsage_Should_Set_Amount_And_Cost()
    {
        var usage = new UsageTracking();

        usage.UpdateUsage(100, 12.5m);

        usage.UsageAmount.Should().Be(100);
        usage.Cost.Should().Be(12.5m);
    }

    [Fact]
    public void AddUsage_Should_Increment_Amount_And_Cost()
    {
        var usage = new UsageTracking { UsageAmount = 10, Cost = 2.5m };

        usage.AddUsage(5, 1.25m);

        usage.UsageAmount.Should().Be(15);
        usage.Cost.Should().Be(3.75m);
    }
}
