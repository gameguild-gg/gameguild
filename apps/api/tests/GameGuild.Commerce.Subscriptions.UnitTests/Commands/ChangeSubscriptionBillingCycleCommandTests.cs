using FluentAssertions;
using GameGuild.ValueObjects;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Commands;

public class ChangeSubscriptionBillingCycleCommandTests
{
    [Fact]
    public void Command_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var subscriptionId = Guid.NewGuid();
        var command = new ChangeSubscriptionBillingCycleCommand(subscriptionId, BillingCycle.Annual);

        // Assert
        command.SubscriptionId.Should().Be(subscriptionId);
        command.NewBillingCycle.Should().Be(BillingCycle.Annual);
    }

    [Fact]
    public void Command_ShouldSupportRecordEquality()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var command1 = new ChangeSubscriptionBillingCycleCommand(subscriptionId, BillingCycle.Monthly);
        var command2 = new ChangeSubscriptionBillingCycleCommand(subscriptionId, BillingCycle.Monthly);

        // Act & Assert
        command1.Should().Be(command2);
        command1.GetHashCode().Should().Be(command2.GetHashCode());
    }

    [Fact]
    public void Command_ShouldNotBeEqualWithDifferentBillingCycles()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var command1 = new ChangeSubscriptionBillingCycleCommand(subscriptionId, BillingCycle.Monthly);
        var command2 = new ChangeSubscriptionBillingCycleCommand(subscriptionId, BillingCycle.Annual);

        // Act & Assert
        command1.Should().NotBe(command2);
    }
}
