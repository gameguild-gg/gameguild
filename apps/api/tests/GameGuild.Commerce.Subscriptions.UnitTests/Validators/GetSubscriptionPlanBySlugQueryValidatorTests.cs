using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Validators;

/// <summary>
/// Tests for GetSubscriptionPlanBySlugQueryValidator
/// </summary>
public class GetSubscriptionPlanBySlugQueryValidatorTests
{
    private readonly GetSubscriptionPlanBySlugQueryValidator _validator;

    public GetSubscriptionPlanBySlugQueryValidatorTests()
    {
        _validator = new GetSubscriptionPlanBySlugQueryValidator();
    }

    #region Valid Queries

    [Fact]
    public void Validate_ShouldPass_WithValidSlug()
    {
        // Arrange
        var query = new GetSubscriptionPlanBySlugQuery(Slug: "pro-plan");

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("a")]
    [InlineData("abc")]
    [InlineData("pro-plan")]
    [InlineData("pro-plan-123")]
    [InlineData("123")]
    [InlineData("abc123")]
    [InlineData("plan1-tier2")]
    public void Validate_ShouldPass_WithValidSlugFormats(string slug)
    {
        // Arrange
        var query = new GetSubscriptionPlanBySlugQuery(Slug: slug);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldPass_WithMaxLengthSlug()
    {
        // Arrange
        var query = new GetSubscriptionPlanBySlugQuery(Slug: new string('a', 50));

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Empty/Null Validation

    [Fact]
    public void Validate_ShouldFail_WhenSlugIsEmpty()
    {
        // Arrange
        var query = new GetSubscriptionPlanBySlugQuery(Slug: "");

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(GetSubscriptionPlanBySlugQuery.Slug) &&
            e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenSlugIsNull()
    {
        // Arrange
        var query = new GetSubscriptionPlanBySlugQuery(Slug: null!);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(GetSubscriptionPlanBySlugQuery.Slug));
    }

    #endregion

    #region Length Validation

    [Fact]
    public void Validate_ShouldFail_WhenSlugExceedsMaxLength()
    {
        // Arrange
        var query = new GetSubscriptionPlanBySlugQuery(Slug: new string('a', 51));

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(GetSubscriptionPlanBySlugQuery.Slug) &&
            e.ErrorMessage.Contains("50 characters"));
    }

    #endregion

    #region Format Validation

    [Theory]
    [InlineData("Pro Plan")] // Contains uppercase and space
    [InlineData("pro_plan")] // Contains underscore
    [InlineData("pro.plan")] // Contains period
    [InlineData("PRO-PLAN")] // Contains uppercase
    [InlineData("pro plan")] // Contains space
    [InlineData("pro@plan")] // Contains @
    [InlineData("pro!plan")] // Contains !
    public void Validate_ShouldFail_WhenSlugHasInvalidCharacters(string slug)
    {
        // Arrange
        var query = new GetSubscriptionPlanBySlugQuery(Slug: slug);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(GetSubscriptionPlanBySlugQuery.Slug) &&
            e.ErrorMessage.Contains("lowercase letters, numbers, and hyphens"));
    }

    #endregion

    #region Multiple Errors

    [Fact]
    public void Validate_ShouldReturnMultipleErrors_WhenSlugHasMultipleIssues()
    {
        // Arrange - slug with invalid characters AND too long
        var invalidSlug = new string('A', 60); // uppercase and too long

        var query = new GetSubscriptionPlanBySlugQuery(Slug: invalidSlug);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(2);
    }

    #endregion
}
