using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Validators;

/// <summary>
/// Tests for RecordSubscriptionPaymentCommandValidator
/// Covers all validation rules for payment recording
/// </summary>
public class RecordSubscriptionPaymentCommandValidatorTests
{
    private readonly RecordSubscriptionPaymentCommandValidator _validator;

    public RecordSubscriptionPaymentCommandValidatorTests()
    {
        _validator = new RecordSubscriptionPaymentCommandValidator();
    }

    #region Valid Commands

    [Fact]
    public void Validate_ShouldPass_WithValidCommand()
    {
        // Arrange
        var command = new RecordSubscriptionPaymentCommand(
            SubscriptionId: Guid.NewGuid(),
            Amount: 99.99m,
            Currency: "USD",
            PaymentDate: DateTime.UtcNow.AddMinutes(-5),
            IdempotencyKey: "payment-key-123",
            ForBillingCycle: 1
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ShouldPass_WithValidCommand_AndBillingCycle()
    {
        // Arrange
        var command = new RecordSubscriptionPaymentCommand(
            SubscriptionId: Guid.NewGuid(),
            Amount: 199.99m,
            Currency: "EUR",
            PaymentDate: DateTime.UtcNow.AddHours(-1),
            IdempotencyKey: "payment-key-456",
            ForBillingCycle: 3
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("GBP")]
    [InlineData("JPY")]
    [InlineData("CHF")]
    public void Validate_ShouldPass_WithDifferentCurrencies(string currency)
    {
        // Arrange
        var command = new RecordSubscriptionPaymentCommand(
            SubscriptionId: Guid.NewGuid(),
            Amount: 100m,
            Currency: currency,
            PaymentDate: DateTime.UtcNow.AddMinutes(-1),
            IdempotencyKey: Guid.NewGuid().ToString(),
            ForBillingCycle: 1
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region SubscriptionId Validation

    [Fact]
    public void Validate_ShouldFail_WhenSubscriptionIdIsEmpty()
    {
        // Arrange
        var command = new RecordSubscriptionPaymentCommand(
            SubscriptionId: Guid.Empty,
            Amount: 99.99m,
            Currency: "USD",
            PaymentDate: DateTime.UtcNow,
            IdempotencyKey: "payment-key-123"
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == nameof(RecordSubscriptionPaymentCommand.SubscriptionId) &&
            e.ErrorMessage.Contains("required"));
    }

    #endregion

    #region Amount Validation

    [Fact]
    public void Validate_ShouldFail_WhenAmountIsZero()
    {
        // Arrange
        var command = new RecordSubscriptionPaymentCommand(
            SubscriptionId: Guid.NewGuid(),
            Amount: 0m,
            Currency: "USD",
            PaymentDate: DateTime.UtcNow,
            IdempotencyKey: "payment-key-123"
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == nameof(RecordSubscriptionPaymentCommand.Amount) &&
            e.ErrorMessage.Contains("greater than 0"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenAmountIsNegative()
    {
        // Arrange
        var command = new RecordSubscriptionPaymentCommand(
            SubscriptionId: Guid.NewGuid(),
            Amount: -50m,
            Currency: "USD",
            PaymentDate: DateTime.UtcNow,
            IdempotencyKey: "payment-key-123"
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == nameof(RecordSubscriptionPaymentCommand.Amount));
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(0.001)]
    [InlineData(1)]
    [InlineData(999999.99)]
    public void Validate_ShouldPass_WithPositiveAmounts(decimal amount)
    {
        // Arrange - use a time safely in the past to avoid race conditions
        var command = new RecordSubscriptionPaymentCommand(
            SubscriptionId: Guid.NewGuid(),
            Amount: amount,
            Currency: "USD",
            PaymentDate: DateTime.UtcNow.AddMinutes(-1),
            IdempotencyKey: Guid.NewGuid().ToString(),
            ForBillingCycle: 1
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Currency Validation

    [Fact]
    public void Validate_ShouldFail_WhenCurrencyIsEmpty()
    {
        // Arrange
        var command = new RecordSubscriptionPaymentCommand(
            SubscriptionId: Guid.NewGuid(),
            Amount: 99.99m,
            Currency: string.Empty,
            PaymentDate: DateTime.UtcNow,
            IdempotencyKey: "payment-key-123"
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == nameof(RecordSubscriptionPaymentCommand.Currency) &&
            e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenCurrencyIsNull()
    {
        // Arrange
        var command = new RecordSubscriptionPaymentCommand(
            SubscriptionId: Guid.NewGuid(),
            Amount: 99.99m,
            Currency: null!,
            PaymentDate: DateTime.UtcNow,
            IdempotencyKey: "payment-key-123"
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == nameof(RecordSubscriptionPaymentCommand.Currency));
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("A")]
    [InlineData("ABCDE")]
    public void Validate_ShouldFail_WhenCurrencyIsNotThreeCharacters(string currency)
    {
        // Arrange
        var command = new RecordSubscriptionPaymentCommand(
            SubscriptionId: Guid.NewGuid(),
            Amount: 99.99m,
            Currency: currency,
            PaymentDate: DateTime.UtcNow,
            IdempotencyKey: "payment-key-123"
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == nameof(RecordSubscriptionPaymentCommand.Currency) &&
            e.ErrorMessage.Contains("3 characters"));
    }

    #endregion

    #region PaymentDate Validation

    [Fact]
    public void Validate_ShouldFail_WhenPaymentDateIsInFuture()
    {
        // Arrange
        var command = new RecordSubscriptionPaymentCommand(
            SubscriptionId: Guid.NewGuid(),
            Amount: 99.99m,
            Currency: "USD",
            PaymentDate: DateTime.UtcNow.AddDays(1),
            IdempotencyKey: "payment-key-123"
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == nameof(RecordSubscriptionPaymentCommand.PaymentDate) &&
            e.ErrorMessage.Contains("future"));
    }

    [Theory]
    [InlineData(-1)] // 1 second ago
    [InlineData(-60)] // 1 minute ago
    [InlineData(-3600)] // 1 hour ago
    [InlineData(-86400)] // 1 day ago
    public void Validate_ShouldPass_WhenPaymentDateIsInPast(int secondsAgo)
    {
        // Arrange
        var command = new RecordSubscriptionPaymentCommand(
            SubscriptionId: Guid.NewGuid(),
            Amount: 99.99m,
            Currency: "USD",
            PaymentDate: DateTime.UtcNow.AddSeconds(secondsAgo),
            IdempotencyKey: Guid.NewGuid().ToString(),
            ForBillingCycle: 1
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region IdempotencyKey Validation

    [Fact]
    public void Validate_ShouldFail_WhenIdempotencyKeyIsEmpty()
    {
        // Arrange
        var command = new RecordSubscriptionPaymentCommand(
            SubscriptionId: Guid.NewGuid(),
            Amount: 99.99m,
            Currency: "USD",
            PaymentDate: DateTime.UtcNow,
            IdempotencyKey: string.Empty
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == nameof(RecordSubscriptionPaymentCommand.IdempotencyKey) &&
            e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenIdempotencyKeyIsNull()
    {
        // Arrange
        var command = new RecordSubscriptionPaymentCommand(
            SubscriptionId: Guid.NewGuid(),
            Amount: 99.99m,
            Currency: "USD",
            PaymentDate: DateTime.UtcNow,
            IdempotencyKey: null!
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == nameof(RecordSubscriptionPaymentCommand.IdempotencyKey));
    }

    [Fact]
    public void Validate_ShouldFail_WhenIdempotencyKeyIsWhitespace()
    {
        // Arrange
        var command = new RecordSubscriptionPaymentCommand(
            SubscriptionId: Guid.NewGuid(),
            Amount: 99.99m,
            Currency: "USD",
            PaymentDate: DateTime.UtcNow,
            IdempotencyKey: "   "
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == nameof(RecordSubscriptionPaymentCommand.IdempotencyKey));
    }

    #endregion

    #region Billing Cycle Identity Validation

    [Fact]
    public void Validate_ShouldFail_WhenBillingCycleIdentityIsNotPositive()
    {
        var command = new RecordSubscriptionPaymentCommand(
            SubscriptionId: Guid.NewGuid(),
            Amount: 99.99m,
            Currency: "USD",
            PaymentDate: DateTime.UtcNow,
            IdempotencyKey: "payment-1",
            ForBillingCycle: 0);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(RecordSubscriptionPaymentCommand.ForBillingCycle));
    }

    #endregion

    #region Multiple Validation Errors

    [Fact]
    public void Validate_ShouldReturnAllErrors_WhenMultipleFieldsInvalid()
    {
        // Arrange
        var command = new RecordSubscriptionPaymentCommand(
            SubscriptionId: Guid.Empty,
            Amount: -100m,
            Currency: "",
            PaymentDate: DateTime.UtcNow.AddDays(5),
            IdempotencyKey: ""
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(4);
    }

    #endregion
}
