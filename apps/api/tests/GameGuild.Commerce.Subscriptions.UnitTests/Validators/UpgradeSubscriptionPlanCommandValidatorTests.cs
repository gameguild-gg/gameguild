using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Validators;

/// <summary>
/// Tests for UpgradeSubscriptionPlanCommandValidator
/// </summary>
public class UpgradeSubscriptionPlanCommandValidatorTests
{
    private readonly UpgradeSubscriptionPlanCommandValidator _validator;

    public UpgradeSubscriptionPlanCommandValidatorTests()
    {
        _validator = new UpgradeSubscriptionPlanCommandValidator();
    }

    [Fact]
    public void Validate_ShouldPass_WithValidCommand()
    {
        // Arrange
        var command = new UpgradeSubscriptionPlanCommand(
            SubscriptionId: Guid.NewGuid(),
            NewPlanId: Guid.NewGuid(),
            EffectiveDate: DateTime.UtcNow.Date.AddDays(1),
            ProrateBilling: true
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
        var command = new UpgradeSubscriptionPlanCommand(
            SubscriptionId: Guid.NewGuid(),
            NewPlanId: Guid.NewGuid(),
            EffectiveDate: null,
            ProrateBilling: false
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
        var command = new UpgradeSubscriptionPlanCommand(
            SubscriptionId: Guid.NewGuid(),
            NewPlanId: Guid.NewGuid(),
            EffectiveDate: DateTime.UtcNow.Date,
            ProrateBilling: true
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
        var command = new UpgradeSubscriptionPlanCommand(
            SubscriptionId: Guid.Empty,
            NewPlanId: Guid.NewGuid(),
            EffectiveDate: null,
            ProrateBilling: false
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(UpgradeSubscriptionPlanCommand.SubscriptionId) &&
            e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenNewPlanIdIsEmpty()
    {
        // Arrange
        var command = new UpgradeSubscriptionPlanCommand(
            SubscriptionId: Guid.NewGuid(),
            NewPlanId: Guid.Empty,
            EffectiveDate: null,
            ProrateBilling: false
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(UpgradeSubscriptionPlanCommand.NewPlanId) &&
            e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenEffectiveDateIsInPast()
    {
        // Arrange
        var command = new UpgradeSubscriptionPlanCommand(
            SubscriptionId: Guid.NewGuid(),
            NewPlanId: Guid.NewGuid(),
            EffectiveDate: DateTime.UtcNow.Date.AddDays(-1),
            ProrateBilling: false
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(UpgradeSubscriptionPlanCommand.EffectiveDate) &&
            e.ErrorMessage.Contains("today or in the future"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenBothIdsAreEmpty()
    {
        // Arrange
        var command = new UpgradeSubscriptionPlanCommand(
            SubscriptionId: Guid.Empty,
            NewPlanId: Guid.Empty,
            EffectiveDate: null,
            ProrateBilling: false
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
    }
}
