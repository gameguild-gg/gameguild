using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Commands;

public class EndSubscriptionTrialCommandTests
{
    [Fact]
    public void Command_ShouldHaveCorrectProperties_WithAllParameters()
    {
        // Arrange & Act
        var subscriptionId = Guid.NewGuid();
        var command = new EndSubscriptionTrialCommand(subscriptionId, ConvertToPaid: false);

        // Assert
        command.SubscriptionId.Should().Be(subscriptionId);
        command.ConvertToPaid.Should().BeFalse();
    }

    [Fact]
    public void Command_ShouldHaveDefaultConvertToPaid()
    {
        // Arrange & Act
        var subscriptionId = Guid.NewGuid();
        var command = new EndSubscriptionTrialCommand(subscriptionId);

        // Assert
        command.SubscriptionId.Should().Be(subscriptionId);
        command.ConvertToPaid.Should().BeTrue();
    }

    [Fact]
    public void Command_ShouldSupportRecordEquality()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var command1 = new EndSubscriptionTrialCommand(subscriptionId, ConvertToPaid: true);
        var command2 = new EndSubscriptionTrialCommand(subscriptionId, ConvertToPaid: true);

        // Act & Assert
        command1.Should().Be(command2);
        command1.GetHashCode().Should().Be(command2.GetHashCode());
    }

    [Fact]
    public void Command_ShouldNotBeEqualWithDifferentConvertToPaid()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var command1 = new EndSubscriptionTrialCommand(subscriptionId, ConvertToPaid: true);
        var command2 = new EndSubscriptionTrialCommand(subscriptionId, ConvertToPaid: false);

        // Act & Assert
        command1.Should().NotBe(command2);
    }
}
