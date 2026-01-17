using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Validators;

/// <summary>
/// Tests for GetSubscriptionPlanByIdQueryValidator
/// </summary>
public class GetSubscriptionPlanByIdQueryValidatorTests
{
    private readonly GetSubscriptionPlanByIdQueryValidator _validator;

    public GetSubscriptionPlanByIdQueryValidatorTests()
    {
        _validator = new GetSubscriptionPlanByIdQueryValidator();
    }

    [Fact]
    public void Validate_ShouldPass_WithValidQuery()
    {
        // Arrange
        var query = new GetSubscriptionPlanByIdQuery(Id: Guid.NewGuid());

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ShouldFail_WhenIdIsEmpty()
    {
        // Arrange
        var query = new GetSubscriptionPlanByIdQuery(Id: Guid.Empty);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(GetSubscriptionPlanByIdQuery.Id) &&
            e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validate_ShouldPass_WithMultipleValidGuids()
    {
        // Arrange & Act & Assert
        for (int i = 0; i < 5; i++)
        {
            var query = new GetSubscriptionPlanByIdQuery(Id: Guid.NewGuid());
            var result = _validator.Validate(query);
            result.IsValid.Should().BeTrue();
        }
    }
}
