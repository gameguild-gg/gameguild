using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Commands;

public class UpdateSubscriptionPlanCommandTests
{
    [Fact]
    public void Command_ShouldHaveCorrectProperties_WithAllParameters()
    {
        // Arrange & Act
        var planId = Guid.NewGuid();
        var command = new UpdateSubscriptionPlanCommand(
            planId,
            Name: "Updated Premium Plan",
            Description: "Updated description",
            SortOrder: 5
        );

        // Assert
        command.Id.Should().Be(planId);
        command.Name.Should().Be("Updated Premium Plan");
        command.Description.Should().Be("Updated description");
        command.SortOrder.Should().Be(5);
    }

    [Fact]
    public void Command_ShouldAllowNullDescription()
    {
        // Arrange & Act
        var planId = Guid.NewGuid();
        var command = new UpdateSubscriptionPlanCommand(
            planId,
            Name: "Plan Name",
            Description: null,
            SortOrder: 1
        );

        // Assert
        command.Description.Should().BeNull();
    }

    [Fact]
    public void Command_ShouldAllowNullSortOrder()
    {
        // Arrange & Act
        var planId = Guid.NewGuid();
        var command = new UpdateSubscriptionPlanCommand(
            planId,
            Name: "Plan Name",
            Description: "Description",
            SortOrder: null
        );

        // Assert
        command.SortOrder.Should().BeNull();
    }

    [Fact]
    public void Command_ShouldSupportRecordEquality()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var command1 = new UpdateSubscriptionPlanCommand(planId, "Name", "Desc", 1);
        var command2 = new UpdateSubscriptionPlanCommand(planId, "Name", "Desc", 1);

        // Act & Assert
        command1.Should().Be(command2);
        command1.GetHashCode().Should().Be(command2.GetHashCode());
    }

    [Fact]
    public void Command_ShouldNotBeEqualWithDifferentNames()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var command1 = new UpdateSubscriptionPlanCommand(planId, "Name 1", "Desc", 1);
        var command2 = new UpdateSubscriptionPlanCommand(planId, "Name 2", "Desc", 1);

        // Act & Assert
        command1.Should().NotBe(command2);
    }
}
