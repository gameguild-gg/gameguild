using FluentAssertions;
using GameGuild.ValueObjects;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Commands;

public class UpdateSubscriptionCommandTests
{
    [Fact]
    public void Command_ShouldHaveCorrectProperties_WithAllParameters()
    {
        // Arrange & Act
        var subscriptionId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var command = new UpdateSubscriptionCommand(
            subscriptionId,
            planId,
            BillingCycle.Monthly,
            Amount: 29.99m,
            AutoRenew: true,
            ExternalSubscriptionId: "sub_123",
            ExternalCustomerId: "cus_456"
        );

        // Assert
        command.SubscriptionId.Should().Be(subscriptionId);
        command.PlanId.Should().Be(planId);
        command.BillingCycle.Should().Be(BillingCycle.Monthly);
        command.Amount.Should().Be(29.99m);
        command.AutoRenew.Should().BeTrue();
        command.ExternalSubscriptionId.Should().Be("sub_123");
        command.ExternalCustomerId.Should().Be("cus_456");
    }

    [Fact]
    public void Command_ShouldHaveDefaultNullExternalIds()
    {
        // Arrange & Act
        var command = new UpdateSubscriptionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            BillingCycle.Annual,
            Amount: 99.99m,
            AutoRenew: false
        );

        // Assert
        command.ExternalSubscriptionId.Should().BeNull();
        command.ExternalCustomerId.Should().BeNull();
    }

    [Fact]
    public void Command_ShouldSupportRecordEquality()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var command1 = new UpdateSubscriptionCommand(subscriptionId, planId, BillingCycle.Monthly, 29.99m, true);
        var command2 = new UpdateSubscriptionCommand(subscriptionId, planId, BillingCycle.Monthly, 29.99m, true);

        // Act & Assert
        command1.Should().Be(command2);
        command1.GetHashCode().Should().Be(command2.GetHashCode());
    }

    [Fact]
    public void Command_ShouldNotBeEqualWithDifferentAmounts()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var command1 = new UpdateSubscriptionCommand(subscriptionId, planId, BillingCycle.Monthly, 29.99m, true);
        var command2 = new UpdateSubscriptionCommand(subscriptionId, planId, BillingCycle.Monthly, 39.99m, true);

        // Act & Assert
        command1.Should().NotBe(command2);
    }
}
