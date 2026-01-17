using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Validators;

/// <summary>
/// Tests for DeactivateSubscriptionPlanCommandValidator
/// </summary>
public class DeactivateSubscriptionPlanCommandValidatorTests
{
    private readonly DeactivateSubscriptionPlanCommandValidator _validator;

    public DeactivateSubscriptionPlanCommandValidatorTests()
    {
        _validator = new DeactivateSubscriptionPlanCommandValidator();
    }

    [Fact]
    public void Validate_ShouldPass_WithValidCommand()
    {
        // Arrange
        var command = new DeactivateSubscriptionPlanCommand(Id: Guid.NewGuid());

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
        var command = new DeactivateSubscriptionPlanCommand(Id: Guid.Empty);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(DeactivateSubscriptionPlanCommand.Id) &&
            e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validate_ShouldPass_WithMultipleValidGuids()
    {
        // Arrange & Act & Assert
        for (int i = 0; i < 5; i++)
        {
            var command = new DeactivateSubscriptionPlanCommand(Id: Guid.NewGuid());
            var result = _validator.Validate(command);
            result.IsValid.Should().BeTrue();
        }
    }
}
