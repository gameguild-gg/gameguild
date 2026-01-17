using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Commands;

public class RecordSubscriptionPaymentFailureCommandTests
{
    [Fact]
    public void Command_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var subscriptionId = Guid.NewGuid();
        var failureDate = new DateTime(2026, 1, 15, 10, 30, 0);
        var command = new RecordSubscriptionPaymentFailureCommand(
            subscriptionId,
            Reason: "Card declined",
            FailureDate: failureDate
        );

        // Assert
        command.SubscriptionId.Should().Be(subscriptionId);
        command.Reason.Should().Be("Card declined");
        command.FailureDate.Should().Be(failureDate);
    }

    [Fact]
    public void Command_ShouldSupportRecordEquality()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var failureDate = DateTime.UtcNow;
        var command1 = new RecordSubscriptionPaymentFailureCommand(subscriptionId, "Card declined", failureDate);
        var command2 = new RecordSubscriptionPaymentFailureCommand(subscriptionId, "Card declined", failureDate);

        // Act & Assert
        command1.Should().Be(command2);
        command1.GetHashCode().Should().Be(command2.GetHashCode());
    }

    [Fact]
    public void Command_ShouldNotBeEqualWithDifferentReasons()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var failureDate = DateTime.UtcNow;
        var command1 = new RecordSubscriptionPaymentFailureCommand(subscriptionId, "Card declined", failureDate);
        var command2 = new RecordSubscriptionPaymentFailureCommand(subscriptionId, "Insufficient funds", failureDate);

        // Act & Assert
        command1.Should().NotBe(command2);
    }
}
