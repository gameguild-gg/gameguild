using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Commands;

public class CloneSubscriptionPlanCommandTests
{
    [Fact]
    public void Command_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var sourcePlanId = Guid.NewGuid();
        var command = new CloneSubscriptionPlanCommand(sourcePlanId, "Premium Plan Copy", "premium-plan-copy");

        // Assert
        command.SourcePlanId.Should().Be(sourcePlanId);
        command.NewName.Should().Be("Premium Plan Copy");
        command.NewSlug.Should().Be("premium-plan-copy");
    }

    [Fact]
    public void Command_ShouldSupportRecordEquality()
    {
        // Arrange
        var sourcePlanId = Guid.NewGuid();
        var command1 = new CloneSubscriptionPlanCommand(sourcePlanId, "Plan Name", "plan-slug");
        var command2 = new CloneSubscriptionPlanCommand(sourcePlanId, "Plan Name", "plan-slug");

        // Act & Assert
        command1.Should().Be(command2);
        command1.GetHashCode().Should().Be(command2.GetHashCode());
    }

    [Fact]
    public void Command_ShouldNotBeEqualWithDifferentNames()
    {
        // Arrange
        var sourcePlanId = Guid.NewGuid();
        var command1 = new CloneSubscriptionPlanCommand(sourcePlanId, "Plan Name 1", "plan-slug");
        var command2 = new CloneSubscriptionPlanCommand(sourcePlanId, "Plan Name 2", "plan-slug");

        // Act & Assert
        command1.Should().NotBe(command2);
    }

    [Fact]
    public void Command_ShouldNotBeEqualWithDifferentSlugs()
    {
        // Arrange
        var sourcePlanId = Guid.NewGuid();
        var command1 = new CloneSubscriptionPlanCommand(sourcePlanId, "Plan Name", "plan-slug-1");
        var command2 = new CloneSubscriptionPlanCommand(sourcePlanId, "Plan Name", "plan-slug-2");

        // Act & Assert
        command1.Should().NotBe(command2);
    }
}
