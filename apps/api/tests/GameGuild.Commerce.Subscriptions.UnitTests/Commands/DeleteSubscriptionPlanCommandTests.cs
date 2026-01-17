using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Commands;

public class DeleteSubscriptionPlanCommandTests
{
    [Fact]
    public void Command_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var planId = Guid.NewGuid();
        var command = new DeleteSubscriptionPlanCommand(planId);

        // Assert
        command.Id.Should().Be(planId);
    }

    [Fact]
    public void Command_ShouldSupportRecordEquality()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var command1 = new DeleteSubscriptionPlanCommand(planId);
        var command2 = new DeleteSubscriptionPlanCommand(planId);

        // Act & Assert
        command1.Should().Be(command2);
        command1.GetHashCode().Should().Be(command2.GetHashCode());
    }

    [Fact]
    public void Command_ShouldNotBeEqualWithDifferentIds()
    {
        // Arrange
        var command1 = new DeleteSubscriptionPlanCommand(Guid.NewGuid());
        var command2 = new DeleteSubscriptionPlanCommand(Guid.NewGuid());

        // Act & Assert
        command1.Should().NotBe(command2);
    }
}
