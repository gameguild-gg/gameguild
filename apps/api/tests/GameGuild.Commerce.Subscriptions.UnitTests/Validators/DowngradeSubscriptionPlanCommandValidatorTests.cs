using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Validators;

/// <summary>
/// Tests for DowngradeSubscriptionPlanCommandValidator
/// </summary>
public class DowngradeSubscriptionPlanCommandValidatorTests
{
    private readonly DowngradeSubscriptionPlanCommandValidator _validator;

    public DowngradeSubscriptionPlanCommandValidatorTests()
    {
        _validator = new DowngradeSubscriptionPlanCommandValidator();
    }

    [Fact]
    public void Validate_ShouldPass_WithValidCommand()
    {
        // Arrange
        var command = new DowngradeSubscriptionPlanCommand(
            SubscriptionId: Guid.NewGuid(),
            NewPlanId: Guid.NewGuid(),
            EffectiveDate: DateTime.UtcNow.Date.AddDays(1)
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ShouldPass_WithValidCommand_NoEffectiveDate()
    {
        // Arrange
        var command = new DowngradeSubscriptionPlanCommand(
            SubscriptionId: Guid.NewGuid(),
            NewPlanId: Guid.NewGuid(),
            EffectiveDate: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldPass_WithTodayAsEffectiveDate()
    {
        // Arrange
        var command = new DowngradeSubscriptionPlanCommand(
            SubscriptionId: Guid.NewGuid(),
            NewPlanId: Guid.NewGuid(),
            EffectiveDate: DateTime.UtcNow.Date
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenSubscriptionIdIsEmpty()
    {
        // Arrange
        var command = new DowngradeSubscriptionPlanCommand(
            SubscriptionId: Guid.Empty,
            NewPlanId: Guid.NewGuid(),
            EffectiveDate: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(DowngradeSubscriptionPlanCommand.SubscriptionId) &&
            e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenNewPlanIdIsEmpty()
    {
        // Arrange
        var command = new DowngradeSubscriptionPlanCommand(
            SubscriptionId: Guid.NewGuid(),
            NewPlanId: Guid.Empty,
            EffectiveDate: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(DowngradeSubscriptionPlanCommand.NewPlanId) &&
            e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenEffectiveDateIsInPast()
    {
        // Arrange
        var command = new DowngradeSubscriptionPlanCommand(
            SubscriptionId: Guid.NewGuid(),
            NewPlanId: Guid.NewGuid(),
            EffectiveDate: DateTime.UtcNow.Date.AddDays(-1)
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(DowngradeSubscriptionPlanCommand.EffectiveDate) &&
            e.ErrorMessage.Contains("today or in the future"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenBothIdsAreEmpty()
    {
        // Arrange
        var command = new DowngradeSubscriptionPlanCommand(
            SubscriptionId: Guid.Empty,
            NewPlanId: Guid.Empty,
            EffectiveDate: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
    }
}
