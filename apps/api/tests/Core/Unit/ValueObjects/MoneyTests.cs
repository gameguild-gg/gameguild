using FluentAssertions;
using Xunit;

namespace GameGuild.Tests.Core.Unit.ValueObjects;

/// <summary>
/// Unit tests for Money value object
/// </summary>
public class MoneyTests
{
    [Fact]
    public void Constructor_Should_Create_Valid_Money()
    {
        // Arrange
        const decimal amount = 100.50m;
        const string currency = "USD";

        // Act
        Money money = new(amount, currency);

        // Assert
        _ = money.Amount.Should().Be(amount);
        _ = money.Currency.Should().Be(currency);
    }

    [Fact]
    public void Constructor_Should_Use_USD_As_Default_Currency()
    {
        // Arrange
        const decimal amount = 100.50m;

        // Act
        Money money = new(amount);

        // Assert
        _ = money.Amount.Should().Be(amount);
        _ = money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Constructor_Should_Round_Amount_To_Two_Decimal_Places()
    {
        // Arrange
        const decimal amount = 100.555m;

        // Act
        Money money = new(amount);

        // Assert
        _ = money.Amount.Should().Be(100.56m);
    }

    [Fact]
    public void Constructor_Should_Normalize_Currency_To_Uppercase()
    {
        // Arrange
        const decimal amount = 100m;
        const string currency = "eur";

        // Act
        Money money = new(amount, currency);

        // Assert
        _ = money.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Constructor_Should_Throw_When_Amount_Is_Negative()
    {
        // Act & Assert
        Action act = () => new Money(-1m);
        _ = act.Should().Throw<ArgumentException>()
            .WithParameterName("amount")
            .WithMessage("*cannot be negative*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Should_Throw_When_Currency_Is_Null_Or_Empty(string? currency)
    {
        // Act & Assert
        Action act = () => new Money(100m, currency!);
        _ = act.Should().Throw<ArgumentException>()
            .WithParameterName("currency")
            .WithMessage("*cannot be null or empty*");
    }

    [Fact]
    public void Zero_Should_Create_Zero_Money()
    {
        // Act
        Money money = Money.Zero();

        // Assert
        _ = money.Amount.Should().Be(0m);
        _ = money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Zero_Should_Create_Zero_Money_With_Specified_Currency()
    {
        // Arrange
        const string currency = "EUR";

        // Act
        Money money = Money.Zero(currency);

        // Assert
        _ = money.Amount.Should().Be(0m);
        _ = money.Currency.Should().Be(currency);
    }

    [Fact]
    public void FromDecimal_Should_Create_Money_With_USD()
    {
        // Arrange
        const decimal amount = 100.50m;

        // Act
        Money money = Money.FromDecimal(amount);

        // Assert
        _ = money.Amount.Should().Be(amount);
        _ = money.Currency.Should().Be("USD");
    }

    [Fact]
    public void FromDecimal_With_Currency_Should_Create_Money_With_Specified_Currency()
    {
        // Arrange
        const decimal amount = 100.50m;
        const string currency = "EUR";

        // Act
        Money money = Money.FromDecimal(amount, currency);

        // Assert
        _ = money.Amount.Should().Be(amount);
        _ = money.Currency.Should().Be(currency);
    }

    [Fact]
    public void Addition_Should_Work_With_Same_Currency()
    {
        // Arrange
        Money money1 = new(100m, "USD");
        Money money2 = new(50m, "USD");

        // Act
        Money result = money1 + money2;

        // Assert
        _ = result.Amount.Should().Be(150m);
        _ = result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Addition_Should_Throw_With_Different_Currencies()
    {
        // Arrange
        Money money1 = new(100m, "USD");
        Money money2 = new(50m, "EUR");

        // Act & Assert
        Action act = () => _ = money1 + money2;
        _ = act.Should().Throw<InvalidOperationException>()
            .WithMessage("*different currencies*");
    }

    [Fact]
    public void Subtraction_Should_Work_With_Same_Currency()
    {
        // Arrange
        Money money1 = new(100m, "USD");
        Money money2 = new(30m, "USD");

        // Act
        Money result = money1 - money2;

        // Assert
        _ = result.Amount.Should().Be(70m);
        _ = result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Subtraction_Should_Throw_With_Different_Currencies()
    {
        // Arrange
        Money money1 = new(100m, "USD");
        Money money2 = new(30m, "EUR");

        // Act & Assert
        Action act = () => _ = money1 - money2;
        _ = act.Should().Throw<InvalidOperationException>()
            .WithMessage("*different currencies*");
    }

    [Fact]
    public void Subtraction_Should_Allow_Negative_Result()
    {
        // Arrange
        Money money1 = new(50m, "USD");
        Money money2 = new(100m, "USD");

        // Act
        Money result = money1 - money2;

        // Assert
        _ = result.Amount.Should().Be(-50m);
        _ = result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Multiplication_Should_Work()
    {
        // Arrange
        Money money = new(100m, "USD");
        const decimal multiplier = 2.5m;

        // Act
        Money result = money * multiplier;

        // Assert
        _ = result.Amount.Should().Be(250m);
        _ = result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Division_Should_Work()
    {
        // Arrange
        Money money = new(100m, "USD");
        const decimal divisor = 4m;

        // Act
        Money result = money / divisor;

        // Assert
        _ = result.Amount.Should().Be(25m);
        _ = result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Division_Should_Throw_When_Divisor_Is_Zero()
    {
        // Arrange
        Money money = new(100m, "USD");

        // Act & Assert
        Action act = () => _ = money / 0m;
        _ = act.Should().Throw<DivideByZeroException>()
            .WithMessage("*divide money by zero*");
    }

    [Fact]
    public void GreaterThan_Should_Work_With_Same_Currency()
    {
        // Arrange
        Money money1 = new(100m, "USD");
        Money money2 = new(50m, "USD");

        // Act & Assert
        _ = (money1 > money2).Should().BeTrue();
        _ = (money2 > money1).Should().BeFalse();
    }

    [Fact]
    public void GreaterThan_Should_Throw_With_Different_Currencies()
    {
        // Arrange
        Money money1 = new(100m, "USD");
        Money money2 = new(50m, "EUR");

        // Act & Assert
        Action act = () => _ = money1 > money2;
        _ = act.Should().Throw<InvalidOperationException>()
            .WithMessage("*different currencies*");
    }

    [Fact]
    public void LessThan_Should_Work_With_Same_Currency()
    {
        // Arrange
        Money money1 = new(50m, "USD");
        Money money2 = new(100m, "USD");

        // Act & Assert
        _ = (money1 < money2).Should().BeTrue();
        _ = (money2 < money1).Should().BeFalse();
    }

    [Fact]
    public void GreaterThanOrEqual_Should_Work()
    {
        // Arrange
        Money money1 = new(100m, "USD");
        Money money2 = new(100m, "USD");
        Money money3 = new(50m, "USD");

        // Act & Assert
        _ = (money1 >= money2).Should().BeTrue();
        _ = (money1 >= money3).Should().BeTrue();
        _ = (money3 >= money1).Should().BeFalse();
    }

    [Fact]
    public void LessThanOrEqual_Should_Work()
    {
        // Arrange
        Money money1 = new(50m, "USD");
        Money money2 = new(50m, "USD");
        Money money3 = new(100m, "USD");

        // Act & Assert
        _ = (money1 <= money2).Should().BeTrue();
        _ = (money1 <= money3).Should().BeTrue();
        _ = (money3 <= money1).Should().BeFalse();
    }

    [Fact]
    public void Explicit_Conversion_From_Decimal_Should_Work()
    {
        // Arrange
        const decimal amount = 100.50m;

        // Act
        Money money = (Money)amount;

        // Assert
        _ = money.Amount.Should().Be(amount);
        _ = money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Explicit_Conversion_To_Decimal_Should_Work()
    {
        // Arrange
        Money money = new(100.50m, "USD");

        // Act
        decimal amount = (decimal)money;

        // Assert
        _ = amount.Should().Be(100.50m);
    }

    [Fact]
    public void ToDecimal_Should_Return_Amount()
    {
        // Arrange
        Money money = new(100.50m, "USD");

        // Act
        decimal amount = money.ToDecimal();

        // Assert
        _ = amount.Should().Be(100.50m);
    }

    [Fact]
    public void ToString_Should_Format_Currency()
    {
        // Arrange
        Money money = new(100.50m, "USD");

        // Act
        string result = money.ToString();

        // Assert
        _ = result.Should().Contain("100.50");
        _ = result.Should().Contain("USD");
    }

    [Fact]
    public void Equality_Should_Work_Correctly_For_Records()
    {
        // Arrange
        Money money1 = new(100m, "USD");
        Money money2 = new(100m, "USD");
        Money money3 = new(100m, "EUR");
        Money money4 = new(50m, "USD");

        // Act & Assert
        _ = money1.Should().Be(money2);
        _ = money1.Should().NotBe(money3); // Different currency
        _ = money1.Should().NotBe(money4); // Different amount
        _ = (money1 == money2).Should().BeTrue();
        _ = (money1 == money3).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_Should_Be_Same_For_Equal_Money()
    {
        // Arrange
        Money money1 = new(100m, "USD");
        Money money2 = new(100m, "USD");

        // Act & Assert
        _ = money1.GetHashCode().Should().Be(money2.GetHashCode());
    }
}