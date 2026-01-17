using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Commands;

public class ArchiveSubscriptionPlanCommandTests
{
    [Fact]
    public void Command_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var planId = Guid.NewGuid();
        var command = new ArchiveSubscriptionPlanCommand(planId);

        // Assert
        command.PlanId.Should().Be(planId);
    }

    [Fact]
    public void Command_ShouldSupportRecordEquality()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var command1 = new ArchiveSubscriptionPlanCommand(planId);
        var command2 = new ArchiveSubscriptionPlanCommand(planId);

        // Act & Assert
        command1.Should().Be(command2);
        command1.GetHashCode().Should().Be(command2.GetHashCode());
    }

    [Fact]
    public void Command_ShouldNotBeEqualWithDifferentIds()
    {
        // Arrange
        var command1 = new ArchiveSubscriptionPlanCommand(Guid.NewGuid());
        var command2 = new ArchiveSubscriptionPlanCommand(Guid.NewGuid());

        // Act & Assert
        command1.Should().NotBe(command2);
    }
}
