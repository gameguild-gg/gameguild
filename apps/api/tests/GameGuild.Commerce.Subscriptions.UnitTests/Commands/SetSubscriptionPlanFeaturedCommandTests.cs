using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Commands;

public class SetSubscriptionPlanFeaturedCommandTests
{
    [Fact]
    public void Command_ShouldHaveCorrectProperties_WhenFeatured()
    {
        // Arrange & Act
        var planId = Guid.NewGuid();
        var command = new SetSubscriptionPlanFeaturedCommand(planId, IsFeatured: true);

        // Assert
        command.Id.Should().Be(planId);
        command.IsFeatured.Should().BeTrue();
    }

    [Fact]
    public void Command_ShouldHaveCorrectProperties_WhenNotFeatured()
    {
        // Arrange & Act
        var planId = Guid.NewGuid();
        var command = new SetSubscriptionPlanFeaturedCommand(planId, IsFeatured: false);

        // Assert
        command.Id.Should().Be(planId);
        command.IsFeatured.Should().BeFalse();
    }

    [Fact]
    public void Command_ShouldSupportRecordEquality()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var command1 = new SetSubscriptionPlanFeaturedCommand(planId, true);
        var command2 = new SetSubscriptionPlanFeaturedCommand(planId, true);

        // Act & Assert
        command1.Should().Be(command2);
        command1.GetHashCode().Should().Be(command2.GetHashCode());
    }

    [Fact]
    public void Command_ShouldNotBeEqualWithDifferentFeaturedValues()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var command1 = new SetSubscriptionPlanFeaturedCommand(planId, true);
        var command2 = new SetSubscriptionPlanFeaturedCommand(planId, false);

        // Act & Assert
        command1.Should().NotBe(command2);
    }
}
