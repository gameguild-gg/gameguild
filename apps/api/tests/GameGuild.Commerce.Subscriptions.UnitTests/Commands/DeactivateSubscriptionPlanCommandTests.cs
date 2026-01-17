using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Commands;

public class DeactivateSubscriptionPlanCommandTests
{
    [Fact]
    public void Command_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var planId = Guid.NewGuid();
        var command = new DeactivateSubscriptionPlanCommand(planId);

        // Assert
        command.Id.Should().Be(planId);
    }

    [Fact]
    public void Command_ShouldSupportRecordEquality()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var command1 = new DeactivateSubscriptionPlanCommand(planId);
        var command2 = new DeactivateSubscriptionPlanCommand(planId);

        // Act & Assert
        command1.Should().Be(command2);
        command1.GetHashCode().Should().Be(command2.GetHashCode());
    }

    [Fact]
    public void Command_ShouldNotBeEqualWithDifferentIds()
    {
        // Arrange
        var command1 = new DeactivateSubscriptionPlanCommand(Guid.NewGuid());
        var command2 = new DeactivateSubscriptionPlanCommand(Guid.NewGuid());

        // Act & Assert
        command1.Should().NotBe(command2);
    }
}
