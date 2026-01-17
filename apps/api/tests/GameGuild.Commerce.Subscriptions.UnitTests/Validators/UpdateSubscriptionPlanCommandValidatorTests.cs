using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Validators;

/// <summary>
/// Tests for UpdateSubscriptionPlanCommandValidator
/// </summary>
public class UpdateSubscriptionPlanCommandValidatorTests
{
    private readonly UpdateSubscriptionPlanCommandValidator _validator;

    public UpdateSubscriptionPlanCommandValidatorTests()
    {
        _validator = new UpdateSubscriptionPlanCommandValidator();
    }

    [Fact]
    public void Validate_ShouldPass_WithValidCommand()
    {
        // Arrange
        var command = new UpdateSubscriptionPlanCommand(
            Id: Guid.NewGuid(),
            Name: "Updated Plan",
            Description: "Updated description"
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ShouldFail_WhenIdIsEmpty()
    {
        // Arrange
        var command = new UpdateSubscriptionPlanCommand(
            Id: Guid.Empty,
            Name: "Updated Plan",
            Description: "Updated description"
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == nameof(UpdateSubscriptionPlanCommand.Id) &&
            e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validate_ShouldPass_WithNullName()
    {
        // Arrange
        var command = new UpdateSubscriptionPlanCommand(
            Id: Guid.NewGuid(),
            Name: null,
            Description: "Updated description"
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldPass_WithNullDescription()
    {
        // Arrange
        var command = new UpdateSubscriptionPlanCommand(
            Id: Guid.NewGuid(),
            Name: "Updated Plan",
            Description: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldPass_WithAllNullOptionalFields()
    {
        // Arrange
        var command = new UpdateSubscriptionPlanCommand(
            Id: Guid.NewGuid(),
            Name: null,
            Description: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
