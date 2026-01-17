using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Commands;

public class FullUpdateSubscriptionPlanCommandTests
{
    [Fact]
    public void Command_ShouldHaveCorrectProperties_WithAllParameters()
    {
        // Arrange & Act
        var planId = Guid.NewGuid();
        var command = new FullUpdateSubscriptionPlanCommand(
            PlanId: planId,
            Name: "Enterprise Plan",
            Slug: "enterprise-plan",
            Description: "Full enterprise features",
            MonthlyPriceInCents: 9999,
            AnnualPriceInCents: 99990,
            MaxUsers: 100,
            MaxStorageMb: 102400,
            MaxApiCallsPerMonth: 1000000,
            HasPrioritySupport: true,
            HasAdvancedAnalytics: true,
            HasCustomBranding: true,
            Features: "Feature1,Feature2",
            SortOrder: 3
        );

        // Assert
        command.PlanId.Should().Be(planId);
        command.Name.Should().Be("Enterprise Plan");
        command.Slug.Should().Be("enterprise-plan");
        command.Description.Should().Be("Full enterprise features");
        command.MonthlyPriceInCents.Should().Be(9999);
        command.AnnualPriceInCents.Should().Be(99990);
        command.MaxUsers.Should().Be(100);
        command.MaxStorageMb.Should().Be(102400);
        command.MaxApiCallsPerMonth.Should().Be(1000000);
        command.HasPrioritySupport.Should().BeTrue();
        command.HasAdvancedAnalytics.Should().BeTrue();
        command.HasCustomBranding.Should().BeTrue();
        command.Features.Should().Be("Feature1,Feature2");
        command.SortOrder.Should().Be(3);
    }

    [Fact]
    public void Command_ShouldAllowNullOptionalParameters()
    {
        // Arrange & Act
        var planId = Guid.NewGuid();
        var command = new FullUpdateSubscriptionPlanCommand(
            PlanId: planId,
            Name: "Basic Plan",
            Slug: "basic-plan",
            Description: null,
            MonthlyPriceInCents: 999,
            AnnualPriceInCents: null,
            MaxUsers: null,
            MaxStorageMb: null,
            MaxApiCallsPerMonth: null,
            HasPrioritySupport: null,
            HasAdvancedAnalytics: null,
            HasCustomBranding: null,
            Features: null,
            SortOrder: null
        );

        // Assert
        command.Description.Should().BeNull();
        command.AnnualPriceInCents.Should().BeNull();
        command.MaxUsers.Should().BeNull();
        command.MaxStorageMb.Should().BeNull();
        command.MaxApiCallsPerMonth.Should().BeNull();
        command.HasPrioritySupport.Should().BeNull();
        command.HasAdvancedAnalytics.Should().BeNull();
        command.HasCustomBranding.Should().BeNull();
        command.Features.Should().BeNull();
        command.SortOrder.Should().BeNull();
    }

    [Fact]
    public void Command_ShouldSupportRecordEquality()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var command1 = new FullUpdateSubscriptionPlanCommand(planId, "Name", "slug", null, 999, null, null, null, null, null, null, null, null, null);
        var command2 = new FullUpdateSubscriptionPlanCommand(planId, "Name", "slug", null, 999, null, null, null, null, null, null, null, null, null);

        // Act & Assert
        command1.Should().Be(command2);
        command1.GetHashCode().Should().Be(command2.GetHashCode());
    }
}
