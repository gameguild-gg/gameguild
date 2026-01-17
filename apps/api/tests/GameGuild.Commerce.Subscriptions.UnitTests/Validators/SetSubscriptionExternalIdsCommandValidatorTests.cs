using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Validators;

/// <summary>
/// Tests for SetSubscriptionExternalIdsCommandValidator
/// </summary>
public class SetSubscriptionExternalIdsCommandValidatorTests
{
    private readonly SetSubscriptionExternalIdsCommandValidator _validator;

    public SetSubscriptionExternalIdsCommandValidatorTests()
    {
        _validator = new SetSubscriptionExternalIdsCommandValidator();
    }

    #region Valid Commands

    [Fact]
    public void Validate_ShouldPass_WithStripeIdOnly()
    {
        // Arrange
        var command = new SetSubscriptionExternalIdsCommand(
            SubscriptionId: Guid.NewGuid(),
            StripeSubscriptionId: "sub_123abc",
            PayPalSubscriptionId: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ShouldPass_WithPayPalIdOnly()
    {
        // Arrange
        var command = new SetSubscriptionExternalIdsCommand(
            SubscriptionId: Guid.NewGuid(),
            StripeSubscriptionId: null,
            PayPalSubscriptionId: "I-BIXGJ3LMWMN1"
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldPass_WithBothIds()
    {
        // Arrange
        var command = new SetSubscriptionExternalIdsCommand(
            SubscriptionId: Guid.NewGuid(),
            StripeSubscriptionId: "sub_123abc",
            PayPalSubscriptionId: "I-BIXGJ3LMWMN1"
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldPass_WithMaxLengthIds()
    {
        // Arrange
        var command = new SetSubscriptionExternalIdsCommand(
            SubscriptionId: Guid.NewGuid(),
            StripeSubscriptionId: new string('a', 255),
            PayPalSubscriptionId: new string('b', 255)
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
        var command = new SetSubscriptionExternalIdsCommand(
            SubscriptionId: Guid.Empty,
            StripeSubscriptionId: "sub_123abc",
            PayPalSubscriptionId: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(SetSubscriptionExternalIdsCommand.SubscriptionId) &&
            e.ErrorMessage.Contains("required"));
    }

    #endregion

    #region External IDs Validation

    [Fact]
    public void Validate_ShouldFail_WhenBothExternalIdsAreNull()
    {
        // Arrange
        var command = new SetSubscriptionExternalIdsCommand(
            SubscriptionId: Guid.NewGuid(),
            StripeSubscriptionId: null,
            PayPalSubscriptionId: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage.Contains("At least one external subscription ID"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenBothExternalIdsAreEmpty()
    {
        // Arrange
        var command = new SetSubscriptionExternalIdsCommand(
            SubscriptionId: Guid.NewGuid(),
            StripeSubscriptionId: "",
            PayPalSubscriptionId: ""
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage.Contains("At least one external subscription ID"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenStripeIdExceedsMaxLength()
    {
        // Arrange
        var command = new SetSubscriptionExternalIdsCommand(
            SubscriptionId: Guid.NewGuid(),
            StripeSubscriptionId: new string('a', 256),
            PayPalSubscriptionId: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(SetSubscriptionExternalIdsCommand.StripeSubscriptionId) &&
            e.ErrorMessage.Contains("255 characters"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenPayPalIdExceedsMaxLength()
    {
        // Arrange
        var command = new SetSubscriptionExternalIdsCommand(
            SubscriptionId: Guid.NewGuid(),
            StripeSubscriptionId: null,
            PayPalSubscriptionId: new string('a', 256)
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(SetSubscriptionExternalIdsCommand.PayPalSubscriptionId) &&
            e.ErrorMessage.Contains("255 characters"));
    }

    #endregion

    #region Multiple Errors

    [Fact]
    public void Validate_ShouldReturnMultipleErrors_WhenMultipleFieldsInvalid()
    {
        // Arrange
        var command = new SetSubscriptionExternalIdsCommand(
            SubscriptionId: Guid.Empty,
            StripeSubscriptionId: null,
            PayPalSubscriptionId: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(2);
    }

    #endregion
}
