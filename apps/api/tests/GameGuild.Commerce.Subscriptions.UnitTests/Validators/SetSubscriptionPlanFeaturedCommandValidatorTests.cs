using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Validators;

/// <summary>
/// Tests for SetSubscriptionPlanFeaturedCommandValidator
/// </summary>
public class SetSubscriptionPlanFeaturedCommandValidatorTests
{
    private readonly SetSubscriptionPlanFeaturedCommandValidator _validator;

    public SetSubscriptionPlanFeaturedCommandValidatorTests()
    {
        _validator = new SetSubscriptionPlanFeaturedCommandValidator();
    }

    [Fact]
    public void Validate_ShouldPass_WithValidCommand_FeaturedTrue()
    {
        // Arrange
        var command = new SetSubscriptionPlanFeaturedCommand(
            Id: Guid.NewGuid(),
            IsFeatured: true
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ShouldPass_WithValidCommand_FeaturedFalse()
    {
        // Arrange
        var command = new SetSubscriptionPlanFeaturedCommand(
            Id: Guid.NewGuid(),
            IsFeatured: false
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
        var command = new SetSubscriptionPlanFeaturedCommand(
            Id: Guid.Empty,
            IsFeatured: true
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(SetSubscriptionPlanFeaturedCommand.Id) &&
            e.ErrorMessage.Contains("required"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Validate_ShouldFail_WhenIdIsEmpty_RegardlessOfFeaturedValue(bool isFeatured)
    {
        // Arrange
        var command = new SetSubscriptionPlanFeaturedCommand(
            Id: Guid.Empty,
            IsFeatured: isFeatured
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
    }

    [Fact]
    public void Validate_ShouldPass_WithMultipleValidGuids()
    {
        // Arrange & Act & Assert
        for (int i = 0; i < 5; i++)
        {
            var command = new SetSubscriptionPlanFeaturedCommand(
                Id: Guid.NewGuid(),
                IsFeatured: i % 2 == 0
            );
            var result = _validator.Validate(command);
            result.IsValid.Should().BeTrue();
        }
    }
}
