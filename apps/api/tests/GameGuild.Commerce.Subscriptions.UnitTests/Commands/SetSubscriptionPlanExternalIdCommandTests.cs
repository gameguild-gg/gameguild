using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Commands;

public class SetSubscriptionPlanExternalIdCommandTests
{
    [Fact]
    public void Command_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var planId = Guid.NewGuid();
        var command = new SetSubscriptionPlanExternalIdCommand(planId, "price_stripe_123");

        // Assert
        command.Id.Should().Be(planId);
        command.ExternalId.Should().Be("price_stripe_123");
    }

    [Fact]
    public void Command_ShouldSupportRecordEquality()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var command1 = new SetSubscriptionPlanExternalIdCommand(planId, "price_123");
        var command2 = new SetSubscriptionPlanExternalIdCommand(planId, "price_123");

        // Act & Assert
        command1.Should().Be(command2);
        command1.GetHashCode().Should().Be(command2.GetHashCode());
    }

    [Fact]
    public void Command_ShouldNotBeEqualWithDifferentExternalIds()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var command1 = new SetSubscriptionPlanExternalIdCommand(planId, "price_123");
        var command2 = new SetSubscriptionPlanExternalIdCommand(planId, "price_456");

        // Act & Assert
        command1.Should().NotBe(command2);
    }
}
