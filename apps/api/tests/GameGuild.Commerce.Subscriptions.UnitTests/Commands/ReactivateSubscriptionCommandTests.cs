using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Commands;

public class ReactivateSubscriptionCommandTests
{
    [Fact]
    public void Command_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var subscriptionId = Guid.NewGuid();
        var command = new ReactivateSubscriptionCommand(subscriptionId);

        // Assert
        command.SubscriptionId.Should().Be(subscriptionId);
    }

    [Fact]
    public void Command_ShouldSupportRecordEquality()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var command1 = new ReactivateSubscriptionCommand(subscriptionId);
        var command2 = new ReactivateSubscriptionCommand(subscriptionId);

        // Act & Assert
        command1.Should().Be(command2);
        command1.GetHashCode().Should().Be(command2.GetHashCode());
    }

    [Fact]
    public void Command_ShouldNotBeEqualWithDifferentIds()
    {
        // Arrange
        var command1 = new ReactivateSubscriptionCommand(Guid.NewGuid());
        var command2 = new ReactivateSubscriptionCommand(Guid.NewGuid());

        // Act & Assert
        command1.Should().NotBe(command2);
    }
}
