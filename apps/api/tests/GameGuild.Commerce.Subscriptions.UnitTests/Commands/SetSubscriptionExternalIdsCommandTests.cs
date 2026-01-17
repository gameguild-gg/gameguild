using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Commands;

public class SetSubscriptionExternalIdsCommandTests
{
    [Fact]
    public void Command_ShouldHaveCorrectProperties_WithAllParameters()
    {
        // Arrange & Act
        var subscriptionId = Guid.NewGuid();
        var command = new SetSubscriptionExternalIdsCommand(
            subscriptionId,
            StripeSubscriptionId: "sub_stripe_123",
            PayPalSubscriptionId: "I-PP_456"
        );

        // Assert
        command.SubscriptionId.Should().Be(subscriptionId);
        command.StripeSubscriptionId.Should().Be("sub_stripe_123");
        command.PayPalSubscriptionId.Should().Be("I-PP_456");
    }

    [Fact]
    public void Command_ShouldAllowNullStripeId()
    {
        // Arrange & Act
        var subscriptionId = Guid.NewGuid();
        var command = new SetSubscriptionExternalIdsCommand(
            subscriptionId,
            StripeSubscriptionId: null,
            PayPalSubscriptionId: "I-PP_456"
        );

        // Assert
        command.StripeSubscriptionId.Should().BeNull();
        command.PayPalSubscriptionId.Should().Be("I-PP_456");
    }

    [Fact]
    public void Command_ShouldAllowNullPayPalId()
    {
        // Arrange & Act
        var subscriptionId = Guid.NewGuid();
        var command = new SetSubscriptionExternalIdsCommand(
            subscriptionId,
            StripeSubscriptionId: "sub_stripe_123",
            PayPalSubscriptionId: null
        );

        // Assert
        command.StripeSubscriptionId.Should().Be("sub_stripe_123");
        command.PayPalSubscriptionId.Should().BeNull();
    }

    [Fact]
    public void Command_ShouldSupportRecordEquality()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var command1 = new SetSubscriptionExternalIdsCommand(subscriptionId, "sub_123", "I-456");
        var command2 = new SetSubscriptionExternalIdsCommand(subscriptionId, "sub_123", "I-456");

        // Act & Assert
        command1.Should().Be(command2);
        command1.GetHashCode().Should().Be(command2.GetHashCode());
    }

    [Fact]
    public void Command_ShouldNotBeEqualWithDifferentStripeIds()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var command1 = new SetSubscriptionExternalIdsCommand(subscriptionId, "sub_123", null);
        var command2 = new SetSubscriptionExternalIdsCommand(subscriptionId, "sub_456", null);

        // Act & Assert
        command1.Should().NotBe(command2);
    }
}
