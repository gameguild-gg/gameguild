using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Validators;

/// <summary>
/// Tests for SetSubscriptionPlanExternalIdCommandValidator
/// </summary>
public class SetSubscriptionPlanExternalIdCommandValidatorTests
{
    private readonly SetSubscriptionPlanExternalIdCommandValidator _validator;

    public SetSubscriptionPlanExternalIdCommandValidatorTests()
    {
        _validator = new SetSubscriptionPlanExternalIdCommandValidator();
    }

    [Fact]
    public void Validate_ShouldPass_WithValidCommand()
    {
        // Arrange
        var command = new SetSubscriptionPlanExternalIdCommand(
            Id: Guid.NewGuid(),
            ExternalId: "ext_plan_123"
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ShouldPass_WithMaxLengthExternalId()
    {
        // Arrange
        var command = new SetSubscriptionPlanExternalIdCommand(
            Id: Guid.NewGuid(),
            ExternalId: new string('a', 100)
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenIdIsEmpty()
    {
        // Arrange
        var command = new SetSubscriptionPlanExternalIdCommand(
            Id: Guid.Empty,
            ExternalId: "ext_plan_123"
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(SetSubscriptionPlanExternalIdCommand.Id) &&
            e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenExternalIdIsEmpty()
    {
        // Arrange
        var command = new SetSubscriptionPlanExternalIdCommand(
            Id: Guid.NewGuid(),
            ExternalId: ""
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(SetSubscriptionPlanExternalIdCommand.ExternalId) &&
            e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenExternalIdIsNull()
    {
        // Arrange
        var command = new SetSubscriptionPlanExternalIdCommand(
            Id: Guid.NewGuid(),
            ExternalId: null!
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(SetSubscriptionPlanExternalIdCommand.ExternalId));
    }

    [Fact]
    public void Validate_ShouldFail_WhenExternalIdExceedsMaxLength()
    {
        // Arrange
        var command = new SetSubscriptionPlanExternalIdCommand(
            Id: Guid.NewGuid(),
            ExternalId: new string('a', 101)
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(SetSubscriptionPlanExternalIdCommand.ExternalId) &&
            e.ErrorMessage.Contains("100 characters"));
    }

    [Fact]
    public void Validate_ShouldReturnMultipleErrors_WhenBothFieldsInvalid()
    {
        // Arrange
        var command = new SetSubscriptionPlanExternalIdCommand(
            Id: Guid.Empty,
            ExternalId: ""
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
    }

    [Theory]
    [InlineData("stripe_plan_123")]
    [InlineData("paypal-plan-id")]
    [InlineData("custom_ext_id")]
    [InlineData("12345")]
    public void Validate_ShouldPass_WithVariousExternalIdFormats(string externalId)
    {
        // Arrange
        var command = new SetSubscriptionPlanExternalIdCommand(
            Id: Guid.NewGuid(),
            ExternalId: externalId
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
