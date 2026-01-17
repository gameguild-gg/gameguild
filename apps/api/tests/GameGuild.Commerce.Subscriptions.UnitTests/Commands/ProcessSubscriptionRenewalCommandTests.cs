using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Commands;

public class ProcessSubscriptionRenewalCommandTests
{
    [Fact]
    public void Command_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var subscriptionId = Guid.NewGuid();
        var command = new ProcessSubscriptionRenewalCommand(subscriptionId);

        // Assert
        command.SubscriptionId.Should().Be(subscriptionId);
    }

    [Fact]
    public void Command_ShouldSupportRecordEquality()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var command1 = new ProcessSubscriptionRenewalCommand(subscriptionId);
        var command2 = new ProcessSubscriptionRenewalCommand(subscriptionId);

        // Act & Assert
        command1.Should().Be(command2);
        command1.GetHashCode().Should().Be(command2.GetHashCode());
    }

    [Fact]
    public void Command_ShouldNotBeEqualWithDifferentIds()
    {
        // Arrange
        var command1 = new ProcessSubscriptionRenewalCommand(Guid.NewGuid());
        var command2 = new ProcessSubscriptionRenewalCommand(Guid.NewGuid());

        // Act & Assert
        command1.Should().NotBe(command2);
    }
}
