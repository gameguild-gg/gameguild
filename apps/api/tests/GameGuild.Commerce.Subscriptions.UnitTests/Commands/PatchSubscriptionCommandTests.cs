using FluentAssertions;
using GameGuild.ValueObjects;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Commands;

public class PatchSubscriptionCommandTests
{
    [Fact]
    public void Command_ShouldHaveCorrectProperties_WithAllParameters()
    {
        // Arrange & Act
        var subscriptionId = Guid.NewGuid();
        var command = new PatchSubscriptionCommand(
            subscriptionId,
            BillingCycle: BillingCycle.Annual,
            AutoRenew: true,
            ExternalSubscriptionId: "sub_123",
            ExternalCustomerId: "cus_456",
            Metadata: "{\"key\": \"value\"}"
        );

        // Assert
        command.SubscriptionId.Should().Be(subscriptionId);
        command.BillingCycle.Should().Be(BillingCycle.Annual);
        command.AutoRenew.Should().BeTrue();
        command.ExternalSubscriptionId.Should().Be("sub_123");
        command.ExternalCustomerId.Should().Be("cus_456");
        command.Metadata.Should().Be("{\"key\": \"value\"}");
    }

    [Fact]
    public void Command_ShouldHaveAllDefaultNullValues()
    {
        // Arrange & Act
        var subscriptionId = Guid.NewGuid();
        var command = new PatchSubscriptionCommand(subscriptionId);

        // Assert
        command.SubscriptionId.Should().Be(subscriptionId);
        command.BillingCycle.Should().BeNull();
        command.AutoRenew.Should().BeNull();
        command.ExternalSubscriptionId.Should().BeNull();
        command.ExternalCustomerId.Should().BeNull();
        command.Metadata.Should().BeNull();
    }

    [Fact]
    public void Command_ShouldSupportPartialUpdate_OnlyBillingCycle()
    {
        // Arrange & Act
        var subscriptionId = Guid.NewGuid();
        var command = new PatchSubscriptionCommand(subscriptionId, BillingCycle: BillingCycle.Monthly);

        // Assert
        command.SubscriptionId.Should().Be(subscriptionId);
        command.BillingCycle.Should().Be(BillingCycle.Monthly);
        command.AutoRenew.Should().BeNull();
    }

    [Fact]
    public void Command_ShouldSupportPartialUpdate_OnlyAutoRenew()
    {
        // Arrange & Act
        var subscriptionId = Guid.NewGuid();
        var command = new PatchSubscriptionCommand(subscriptionId, AutoRenew: false);

        // Assert
        command.SubscriptionId.Should().Be(subscriptionId);
        command.BillingCycle.Should().BeNull();
        command.AutoRenew.Should().BeFalse();
    }

    [Fact]
    public void Command_ShouldSupportRecordEquality()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var command1 = new PatchSubscriptionCommand(subscriptionId, BillingCycle: BillingCycle.Monthly);
        var command2 = new PatchSubscriptionCommand(subscriptionId, BillingCycle: BillingCycle.Monthly);

        // Act & Assert
        command1.Should().Be(command2);
        command1.GetHashCode().Should().Be(command2.GetHashCode());
    }

    [Fact]
    public void Command_ShouldNotBeEqualWithDifferentPatches()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var command1 = new PatchSubscriptionCommand(subscriptionId, BillingCycle: BillingCycle.Monthly);
        var command2 = new PatchSubscriptionCommand(subscriptionId, BillingCycle: BillingCycle.Annual);

        // Act & Assert
        command1.Should().NotBe(command2);
    }
}
