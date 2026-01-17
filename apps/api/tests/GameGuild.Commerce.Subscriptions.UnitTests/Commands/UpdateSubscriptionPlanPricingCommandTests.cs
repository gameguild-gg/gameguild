using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Commands;

public class UpdateSubscriptionPlanPricingCommandTests
{
    [Fact]
    public void Command_ShouldHaveCorrectProperties_WithAllParameters()
    {
        // Arrange & Act
        var planId = Guid.NewGuid();
        var command = new UpdateSubscriptionPlanPricingCommand(
            planId,
            MonthlyPriceInCents: 2999,
            AnnualPriceInCents: 29990
        );

        // Assert
        command.Id.Should().Be(planId);
        command.MonthlyPriceInCents.Should().Be(2999);
        command.AnnualPriceInCents.Should().Be(29990);
    }

    [Fact]
    public void Command_ShouldHaveDefaultNullAnnualPrice()
    {
        // Arrange & Act
        var planId = Guid.NewGuid();
        var command = new UpdateSubscriptionPlanPricingCommand(planId, MonthlyPriceInCents: 999);

        // Assert
        command.AnnualPriceInCents.Should().BeNull();
    }

    [Fact]
    public void Command_ShouldSupportRecordEquality()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var command1 = new UpdateSubscriptionPlanPricingCommand(planId, 999, 9990);
        var command2 = new UpdateSubscriptionPlanPricingCommand(planId, 999, 9990);

        // Act & Assert
        command1.Should().Be(command2);
        command1.GetHashCode().Should().Be(command2.GetHashCode());
    }

    [Fact]
    public void Command_ShouldNotBeEqualWithDifferentPrices()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var command1 = new UpdateSubscriptionPlanPricingCommand(planId, 999);
        var command2 = new UpdateSubscriptionPlanPricingCommand(planId, 1999);

        // Act & Assert
        command1.Should().NotBe(command2);
    }
}
