using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Commands;

public class UpdateSubscriptionPlanLimitsCommandTests
{
    [Fact]
    public void Command_ShouldHaveCorrectProperties_WithAllParameters()
    {
        // Arrange & Act
        var planId = Guid.NewGuid();
        var command = new UpdateSubscriptionPlanLimitsCommand(
            planId,
            MaxUsers: 50,
            MaxStorageMb: 51200,
            MaxApiCallsPerMonth: 500000
        );

        // Assert
        command.Id.Should().Be(planId);
        command.MaxUsers.Should().Be(50);
        command.MaxStorageMb.Should().Be(51200);
        command.MaxApiCallsPerMonth.Should().Be(500000);
    }

    [Fact]
    public void Command_ShouldAllowAllNullLimits()
    {
        // Arrange & Act
        var planId = Guid.NewGuid();
        var command = new UpdateSubscriptionPlanLimitsCommand(
            planId,
            MaxUsers: null,
            MaxStorageMb: null,
            MaxApiCallsPerMonth: null
        );

        // Assert
        command.MaxUsers.Should().BeNull();
        command.MaxStorageMb.Should().BeNull();
        command.MaxApiCallsPerMonth.Should().BeNull();
    }

    [Fact]
    public void Command_ShouldSupportPartialUpdate_OnlyMaxUsers()
    {
        // Arrange & Act
        var planId = Guid.NewGuid();
        var command = new UpdateSubscriptionPlanLimitsCommand(planId, MaxUsers: 100, null, null);

        // Assert
        command.MaxUsers.Should().Be(100);
        command.MaxStorageMb.Should().BeNull();
        command.MaxApiCallsPerMonth.Should().BeNull();
    }

    [Fact]
    public void Command_ShouldSupportPartialUpdate_OnlyStorage()
    {
        // Arrange & Act
        var planId = Guid.NewGuid();
        var command = new UpdateSubscriptionPlanLimitsCommand(planId, null, MaxStorageMb: 102400, null);

        // Assert
        command.MaxUsers.Should().BeNull();
        command.MaxStorageMb.Should().Be(102400);
        command.MaxApiCallsPerMonth.Should().BeNull();
    }

    [Fact]
    public void Command_ShouldSupportRecordEquality()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var command1 = new UpdateSubscriptionPlanLimitsCommand(planId, 10, 1024, 10000);
        var command2 = new UpdateSubscriptionPlanLimitsCommand(planId, 10, 1024, 10000);

        // Act & Assert
        command1.Should().Be(command2);
        command1.GetHashCode().Should().Be(command2.GetHashCode());
    }

    [Fact]
    public void Command_ShouldNotBeEqualWithDifferentLimits()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var command1 = new UpdateSubscriptionPlanLimitsCommand(planId, 10, null, null);
        var command2 = new UpdateSubscriptionPlanLimitsCommand(planId, 20, null, null);

        // Act & Assert
        command1.Should().NotBe(command2);
    }
}
