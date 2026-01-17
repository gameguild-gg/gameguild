using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Commands;

public class StartSubscriptionTrialCommandTests
{
    [Fact]
    public void Command_ShouldHaveCorrectProperties_WithAllParameters()
    {
        // Arrange & Act
        var subscriptionId = Guid.NewGuid();
        var command = new StartSubscriptionTrialCommand(subscriptionId, TrialDays: 14);

        // Assert
        command.SubscriptionId.Should().Be(subscriptionId);
        command.TrialDays.Should().Be(14);
    }

    [Fact]
    public void Command_ShouldHaveDefaultTrialDays()
    {
        // Arrange & Act
        var subscriptionId = Guid.NewGuid();
        var command = new StartSubscriptionTrialCommand(subscriptionId);

        // Assert
        command.SubscriptionId.Should().Be(subscriptionId);
        command.TrialDays.Should().Be(30);
    }

    [Fact]
    public void Command_ShouldSupportRecordEquality()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var command1 = new StartSubscriptionTrialCommand(subscriptionId, TrialDays: 14);
        var command2 = new StartSubscriptionTrialCommand(subscriptionId, TrialDays: 14);

        // Act & Assert
        command1.Should().Be(command2);
        command1.GetHashCode().Should().Be(command2.GetHashCode());
    }

    [Fact]
    public void Command_ShouldNotBeEqualWithDifferentTrialDays()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var command1 = new StartSubscriptionTrialCommand(subscriptionId, TrialDays: 14);
        var command2 = new StartSubscriptionTrialCommand(subscriptionId, TrialDays: 30);

        // Act & Assert
        command1.Should().NotBe(command2);
    }
}
