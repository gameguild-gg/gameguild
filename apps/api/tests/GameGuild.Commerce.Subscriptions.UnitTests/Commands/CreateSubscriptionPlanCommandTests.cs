using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Commands;

public class CreateSubscriptionPlanCommandTests
{
    [Fact]
    public void Command_ShouldHaveCorrectProperties_WithAllParameters()
    {
        // Arrange & Act
        var command = new CreateSubscriptionPlanCommand(
            Name: "Premium Plan",
            Slug: "premium-plan",
            MonthlyPriceInCents: 2999,
            Currency: "EUR",
            Description: "The premium plan with all features"
        );

        // Assert
        command.Name.Should().Be("Premium Plan");
        command.Slug.Should().Be("premium-plan");
        command.MonthlyPriceInCents.Should().Be(2999);
        command.Currency.Should().Be("EUR");
        command.Description.Should().Be("The premium plan with all features");
    }

    [Fact]
    public void Command_ShouldHaveDefaultUsdCurrency()
    {
        // Arrange & Act
        var command = new CreateSubscriptionPlanCommand(
            Name: "Basic Plan",
            Slug: "basic-plan",
            MonthlyPriceInCents: 999
        );

        // Assert
        command.Currency.Should().Be("USD");
    }

    [Fact]
    public void Command_ShouldHaveDefaultNullDescription()
    {
        // Arrange & Act
        var command = new CreateSubscriptionPlanCommand(
            Name: "Basic Plan",
            Slug: "basic-plan",
            MonthlyPriceInCents: 999
        );

        // Assert
        command.Description.Should().BeNull();
    }

    [Fact]
    public void Command_ShouldSupportRecordEquality()
    {
        // Arrange
        var command1 = new CreateSubscriptionPlanCommand("Plan", "plan", 999, "USD", "Desc");
        var command2 = new CreateSubscriptionPlanCommand("Plan", "plan", 999, "USD", "Desc");

        // Act & Assert
        command1.Should().Be(command2);
        command1.GetHashCode().Should().Be(command2.GetHashCode());
    }

    [Fact]
    public void Command_ShouldNotBeEqualWithDifferentPrices()
    {
        // Arrange
        var command1 = new CreateSubscriptionPlanCommand("Plan", "plan", 999);
        var command2 = new CreateSubscriptionPlanCommand("Plan", "plan", 1999);

        // Act & Assert
        command1.Should().NotBe(command2);
    }

    [Fact]
    public void Command_ShouldNotBeEqualWithDifferentCurrencies()
    {
        // Arrange
        var command1 = new CreateSubscriptionPlanCommand("Plan", "plan", 999, "USD");
        var command2 = new CreateSubscriptionPlanCommand("Plan", "plan", 999, "EUR");

        // Act & Assert
        command1.Should().NotBe(command2);
    }
}
