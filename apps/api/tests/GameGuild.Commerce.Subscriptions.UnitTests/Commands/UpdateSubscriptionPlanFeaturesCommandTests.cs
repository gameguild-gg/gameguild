using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Commands;

public class UpdateSubscriptionPlanFeaturesCommandTests
{
    [Fact]
    public void Command_ShouldHaveCorrectProperties_WithAllParameters()
    {
        // Arrange & Act
        var planId = Guid.NewGuid();
        var command = new UpdateSubscriptionPlanFeaturesCommand(
            planId,
            HasPrioritySupport: true,
            HasAdvancedAnalytics: true,
            HasCustomBranding: true,
            Features: "Feature1,Feature2,Feature3"
        );

        // Assert
        command.Id.Should().Be(planId);
        command.HasPrioritySupport.Should().BeTrue();
        command.HasAdvancedAnalytics.Should().BeTrue();
        command.HasCustomBranding.Should().BeTrue();
        command.Features.Should().Be("Feature1,Feature2,Feature3");
    }

    [Fact]
    public void Command_ShouldAllowAllNullFeatureFlags()
    {
        // Arrange & Act
        var planId = Guid.NewGuid();
        var command = new UpdateSubscriptionPlanFeaturesCommand(
            planId,
            HasPrioritySupport: null,
            HasAdvancedAnalytics: null,
            HasCustomBranding: null,
            Features: null
        );

        // Assert
        command.HasPrioritySupport.Should().BeNull();
        command.HasAdvancedAnalytics.Should().BeNull();
        command.HasCustomBranding.Should().BeNull();
        command.Features.Should().BeNull();
    }

    [Fact]
    public void Command_ShouldSupportPartialUpdate_OnlyPrioritySupport()
    {
        // Arrange & Act
        var planId = Guid.NewGuid();
        var command = new UpdateSubscriptionPlanFeaturesCommand(planId, HasPrioritySupport: true, null, null, null);

        // Assert
        command.HasPrioritySupport.Should().BeTrue();
        command.HasAdvancedAnalytics.Should().BeNull();
        command.HasCustomBranding.Should().BeNull();
    }

    [Fact]
    public void Command_ShouldSupportRecordEquality()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var command1 = new UpdateSubscriptionPlanFeaturesCommand(planId, true, true, false, "Features");
        var command2 = new UpdateSubscriptionPlanFeaturesCommand(planId, true, true, false, "Features");

        // Act & Assert
        command1.Should().Be(command2);
        command1.GetHashCode().Should().Be(command2.GetHashCode());
    }

    [Fact]
    public void Command_ShouldNotBeEqualWithDifferentFeatures()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var command1 = new UpdateSubscriptionPlanFeaturesCommand(planId, null, null, null, "Feature1");
        var command2 = new UpdateSubscriptionPlanFeaturesCommand(planId, null, null, null, "Feature2");

        // Act & Assert
        command1.Should().NotBe(command2);
    }
}
