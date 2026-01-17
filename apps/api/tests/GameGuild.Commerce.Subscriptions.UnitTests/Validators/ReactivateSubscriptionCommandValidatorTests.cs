using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Validators;

/// <summary>
/// Tests for ReactivateSubscriptionCommandValidator
/// </summary>
public class ReactivateSubscriptionCommandValidatorTests
{
    private readonly ReactivateSubscriptionCommandValidator _validator;

    public ReactivateSubscriptionCommandValidatorTests()
    {
        _validator = new ReactivateSubscriptionCommandValidator();
    }

    [Fact]
    public void Validate_ShouldPass_WithValidCommand()
    {
        // Arrange
        var command = new ReactivateSubscriptionCommand(
            SubscriptionId: Guid.NewGuid()
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ShouldFail_WhenSubscriptionIdIsEmpty()
    {
        // Arrange
        var command = new ReactivateSubscriptionCommand(
            SubscriptionId: Guid.Empty
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(ReactivateSubscriptionCommand.SubscriptionId) &&
            e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validate_ShouldPass_WithMultipleValidGuids()
    {
        // Arrange & Act & Assert
        for (int i = 0; i < 5; i++)
        {
            var command = new ReactivateSubscriptionCommand(SubscriptionId: Guid.NewGuid());
            var result = _validator.Validate(command);
            result.IsValid.Should().BeTrue();
        }
    }
}
