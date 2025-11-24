using FluentAssertions;
using Xunit;

namespace GameGuild.Tests.SharedKernel.Unit;

/// <summary>
/// Unit tests for Money value object
/// </summary>
public class MoneyTests
{
    [Theory]
    [InlineData(100, "USD")]
    [InlineData(0, "EUR")]
    [InlineData(99.99, "GBP")]
    public void Constructor_WithValidValues_ShouldCreateMoney(decimal amount, string currency)
    {
        // Act
        var money = new Money(amount, currency);

        // Assert
        money.Should().NotBeNull();
        money.Amount.Should().Be(Math.Round(amount, 2));
        money.Currency.Should().Be(currency.ToUpperInvariant());
    }

    [Fact]
    public void Constructor_WithNegativeAmount_ShouldThrowArgumentException()
    {
        // Act
        var act = () => new Money(-10, "USD");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Amount cannot be negative*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrEmptyCurrency_ShouldThrowArgumentException(string? currency)
    {
        // Act
        var act = () => new Money(100, currency!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Currency cannot be null or empty*");
    }

    [Theory]
    [InlineData(10.123, 10.12)]
    [InlineData(10.125, 10.13)]
    [InlineData(10.999, 11.00)]
    public void Constructor_ShouldRoundToTwoDecimalPlaces(decimal input, decimal expected)
    {
        // Act
        var money = new Money(input, "USD");

        // Assert
        money.Amount.Should().Be(expected);
    }

    [Fact]
    public void Zero_ShouldCreateZeroMoney()
    {
        // Act
        var money = Money.Zero("USD");

        // Assert
        money.Amount.Should().Be(0);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Addition_SameCurrency_ShouldAddAmounts()
    {
        // Arrange
        var money1 = new Money(10.50m, "USD");
        var money2 = new Money(5.25m, "USD");

        // Act
        var result = money1 + money2;

        // Assert
        result.Amount.Should().Be(15.75m);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Addition_DifferentCurrencies_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var money1 = new Money(10, "USD");
        var money2 = new Money(5, "EUR");

        // Act
        var act = () => money1 + money2;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot add money with different currencies*");
    }

    [Fact]
    public void Subtraction_SameCurrency_ShouldSubtractAmounts()
    {
        // Arrange
        var money1 = new Money(20.00m, "USD");
        var money2 = new Money(7.50m, "USD");

        // Act
        var result = money1 - money2;

        // Assert
        result.Amount.Should().Be(12.50m);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Subtraction_DifferentCurrencies_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var money1 = new Money(20, "USD");
        var money2 = new Money(10, "EUR");

        // Act
        var act = () => money1 - money2;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot subtract money with different currencies*");
    }

    [Fact]
    public void Multiplication_ShouldMultiplyAmount()
    {
        // Arrange
        var money = new Money(10.50m, "USD");

        // Act
        var result = money * 3;

        // Assert
        result.Amount.Should().Be(31.50m);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Division_ByNonZero_ShouldDivideAmount()
    {
        // Arrange
        var money = new Money(30.00m, "USD");

        // Act
        var result = money / 3;

        // Assert
        result.Amount.Should().Be(10.00m);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Division_ByZero_ShouldThrowDivideByZeroException()
    {
        // Arrange
        var money = new Money(30, "USD");

        // Act
        var act = () => money / 0;

        // Assert
        act.Should().Throw<DivideByZeroException>()
            .WithMessage("*Cannot divide money by zero*");
    }

    [Theory]
    [InlineData(20, 10, true)]
    [InlineData(10, 20, false)]
    [InlineData(10, 10, false)]
    public void GreaterThan_SameCurrency_ShouldCompareCorrectly(decimal amount1, decimal amount2, bool expected)
    {
        // Arrange
        var money1 = new Money(amount1, "USD");
        var money2 = new Money(amount2, "USD");

        // Act
        var result = money1 > money2;

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GreaterThan_DifferentCurrencies_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var money1 = new Money(20, "USD");
        var money2 = new Money(10, "EUR");

        // Act
        var act = () => money1 > money2;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot compare money with different currencies*");
    }

    [Theory]
    [InlineData(10, 20, true)]
    [InlineData(20, 10, false)]
    [InlineData(10, 10, false)]
    public void LessThan_SameCurrency_ShouldCompareCorrectly(decimal amount1, decimal amount2, bool expected)
    {
        // Arrange
        var money1 = new Money(amount1, "USD");
        var money2 = new Money(amount2, "USD");

        // Act
        var result = money1 < money2;

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ToString_ShouldFormatCorrectly()
    {
        // Arrange
        var money = new Money(123.45m, "USD");

        // Act
        var result = money.ToString();

        // Assert - Should contain the currency and amount (culture-dependent format)
        result.Should().Contain("USD");
        result.Should().MatchRegex(@"123[.,]45"); // Matches both 123.45 and 123,45
    }

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(100, "USD");

        // Act & Assert
        money1.Should().Be(money2);
        (money1 == money2).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(200, "USD");

        // Act & Assert
        money1.Should().NotBe(money2);
        (money1 != money2).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentCurrencies_ShouldNotBeEqual()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(100, "EUR");

        // Act & Assert
        money1.Should().NotBe(money2);
        (money1 != money2).Should().BeTrue();
    }

    [Fact]
    public void Zero_WithZeroAmount_ShouldEqualZeroFactory()
    {
        // Arrange & Act
        var money = new Money(0, "USD");

        // Assert
        money.Amount.Should().Be(0);
        money.Should().Be(Money.Zero("USD"));
    }

    [Theory]
    [InlineData("usd")]
    [InlineData("Usd")]
    [InlineData("USD")]
    public void Constructor_ShouldNormalizeCurrency(string currency)
    {
        // Act
        var money = new Money(100, currency);

        // Assert
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void GreaterThanOrEqual_ShouldWorkCorrectly()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(100, "USD");
        var money3 = new Money(50, "USD");

        // Act & Assert
        (money1 >= money2).Should().BeTrue();
        (money1 >= money3).Should().BeTrue();
        (money3 >= money1).Should().BeFalse();
    }

    [Fact]
    public void LessThanOrEqual_ShouldWorkCorrectly()
    {
        // Arrange
        var money1 = new Money(50, "USD");
        var money2 = new Money(50, "USD");
        var money3 = new Money(100, "USD");

        // Act & Assert
        (money1 <= money2).Should().BeTrue();
        (money1 <= money3).Should().BeTrue();
        (money3 <= money1).Should().BeFalse();
    }

    [Theory]
    [InlineData(100.00, 2, 50.00)]
    [InlineData(99.99, 3, 33.33)]
    [InlineData(10, 4, 2.5)]
    public void Division_WithPositiveDivisor_ShouldDivideCorrectly(decimal amount, decimal divisor, decimal expected)
    {
        // Arrange
        var money = new Money(amount, "USD");

        // Act
        var result = money / divisor;

        // Assert
        result.Amount.Should().Be(expected);
        result.Currency.Should().Be("USD");
    }

    [Theory]
    [InlineData(10.55, 10.55)]
    [InlineData(20.555, 20.56)]
    [InlineData(30.554, 30.55)]
    public void Constructor_WithDecimalPrecision_ShouldRoundCorrectly(decimal input, decimal expected)
    {
        // Act
        var money = new Money(input, "USD");

        // Assert
        money.Amount.Should().Be(expected);
    }

    [Fact]
    public void ChainedOperations_ShouldMaintainAccuracy()
    {
        // Arrange
        var initial = new Money(100, "USD");

        // Act
        var result = (initial + new Money(50, "USD")) * 2 - new Money(100, "USD");

        // Assert
        result.Amount.Should().Be(200);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void ValueObject_WithSameValues_ShouldHaveSameHashCode()
    {
        // Arrange
        var money1 = new Money(123.45m, "USD");
        var money2 = new Money(123.45m, "USD");

        // Act & Assert
        money1.GetHashCode().Should().Be(money2.GetHashCode());
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("GBP")]
    [InlineData("JPY")]
    [InlineData("BRL")]
    public void Constructor_WithDifferentCurrencies_ShouldSucceed(string currency)
    {
        // Act
        var money = new Money(100, currency);

        // Assert
        money.Currency.Should().Be(currency.ToUpperInvariant());
        money.Amount.Should().Be(100);
    }
}
