using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Commands;

public class DowngradeSubscriptionPlanCommandTests
{
    [Fact]
    public void Command_ShouldHaveCorrectProperties_WithAllParameters()
    {
        // Arrange & Act
        var subscriptionId = Guid.NewGuid();
        var newPlanId = Guid.NewGuid();
        var effectiveDate = new DateTime(2026, 2, 1);
        var command = new DowngradeSubscriptionPlanCommand(subscriptionId, newPlanId, effectiveDate);

        // Assert
        command.SubscriptionId.Should().Be(subscriptionId);
        command.NewPlanId.Should().Be(newPlanId);
        command.EffectiveDate.Should().Be(effectiveDate);
    }

    [Fact]
    public void Command_ShouldHaveDefaultNullEffectiveDate()
    {
        // Arrange & Act
        var subscriptionId = Guid.NewGuid();
        var newPlanId = Guid.NewGuid();
        var command = new DowngradeSubscriptionPlanCommand(subscriptionId, newPlanId);

        // Assert
        command.SubscriptionId.Should().Be(subscriptionId);
        command.NewPlanId.Should().Be(newPlanId);
        command.EffectiveDate.Should().BeNull();
    }

    [Fact]
    public void Command_ShouldSupportRecordEquality()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var newPlanId = Guid.NewGuid();
        var effectiveDate = DateTime.UtcNow;
        var command1 = new DowngradeSubscriptionPlanCommand(subscriptionId, newPlanId, effectiveDate);
        var command2 = new DowngradeSubscriptionPlanCommand(subscriptionId, newPlanId, effectiveDate);

        // Act & Assert
        command1.Should().Be(command2);
        command1.GetHashCode().Should().Be(command2.GetHashCode());
    }

    [Fact]
    public void Command_ShouldNotBeEqualWithDifferentNewPlanIds()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var command1 = new DowngradeSubscriptionPlanCommand(subscriptionId, Guid.NewGuid());
        var command2 = new DowngradeSubscriptionPlanCommand(subscriptionId, Guid.NewGuid());

        // Act & Assert
        command1.Should().NotBe(command2);
    }
}
