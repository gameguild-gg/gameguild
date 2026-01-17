using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Validators;

/// <summary>
/// Tests for CreateSubscriptionPlanCommandValidator
/// Covers all validation rules for plan creation
/// </summary>
public class CreateSubscriptionPlanCommandValidatorTests
{
    private readonly CreateSubscriptionPlanCommandValidator _validator;

    public CreateSubscriptionPlanCommandValidatorTests()
    {
        _validator = new CreateSubscriptionPlanCommandValidator();
    }

    #region Valid Commands

    [Fact]
    public void Validate_ShouldPass_WithValidCommand()
    {
        // Arrange
        var command = new CreateSubscriptionPlanCommand(
            Name: "Pro Plan",
            Slug: "pro-plan",
            MonthlyPriceInCents: 999,
            Currency: "USD",
            Description: "Our professional plan"
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ShouldPass_WithMinimalValidCommand()
    {
        // Arrange
        var command = new CreateSubscriptionPlanCommand(
            Name: "A",
            Slug: "a",
            MonthlyPriceInCents: 0,
            Currency: "USD",
            Description: null
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
        var command = new CreateSubscriptionPlanCommand(
            Name: "Basic Plan",
            Slug: "basic-plan",
            MonthlyPriceInCents: 500,
            Currency: "EUR",
            Description: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldPass_WithEmptyDescription()
    {
        // Arrange
        var command = new CreateSubscriptionPlanCommand(
            Name: "Basic Plan",
            Slug: "basic-plan",
            MonthlyPriceInCents: 500,
            Currency: "GBP",
            Description: ""
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Name Validation

    [Fact]
    public void Validate_ShouldFail_WhenNameIsEmpty()
    {
        // Arrange
        var command = new CreateSubscriptionPlanCommand(
            Name: "",
            Slug: "valid-slug",
            MonthlyPriceInCents: 999,
            Currency: "USD",
            Description: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == nameof(CreateSubscriptionPlanCommand.Name) &&
            e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenNameIsNull()
    {
        // Arrange
        var command = new CreateSubscriptionPlanCommand(
            Name: null!,
            Slug: "valid-slug",
            MonthlyPriceInCents: 999,
            Currency: "USD",
            Description: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == nameof(CreateSubscriptionPlanCommand.Name));
    }

    [Fact]
    public void Validate_ShouldFail_WhenNameExceeds100Characters()
    {
        // Arrange
        var command = new CreateSubscriptionPlanCommand(
            Name: new string('a', 101),
            Slug: "valid-slug",
            MonthlyPriceInCents: 999,
            Currency: "USD",
            Description: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == nameof(CreateSubscriptionPlanCommand.Name) &&
            e.ErrorMessage.Contains("100 characters"));
    }

    [Fact]
    public void Validate_ShouldPass_WhenNameIsExactly100Characters()
    {
        // Arrange
        var command = new CreateSubscriptionPlanCommand(
            Name: new string('a', 100),
            Slug: "valid-slug",
            MonthlyPriceInCents: 999,
            Currency: "USD",
            Description: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Slug Validation

    [Fact]
    public void Validate_ShouldFail_WhenSlugIsEmpty()
    {
        // Arrange
        var command = new CreateSubscriptionPlanCommand(
            Name: "Valid Name",
            Slug: "",
            MonthlyPriceInCents: 999,
            Currency: "USD",
            Description: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == nameof(CreateSubscriptionPlanCommand.Slug) &&
            e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenSlugIsNull()
    {
        // Arrange
        var command = new CreateSubscriptionPlanCommand(
            Name: "Valid Name",
            Slug: null!,
            MonthlyPriceInCents: 999,
            Currency: "USD",
            Description: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == nameof(CreateSubscriptionPlanCommand.Slug));
    }

    [Fact]
    public void Validate_ShouldFail_WhenSlugExceeds50Characters()
    {
        // Arrange
        var command = new CreateSubscriptionPlanCommand(
            Name: "Valid Name",
            Slug: new string('a', 51),
            MonthlyPriceInCents: 999,
            Currency: "USD",
            Description: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == nameof(CreateSubscriptionPlanCommand.Slug) &&
            e.ErrorMessage.Contains("50 characters"));
    }

    [Fact]
    public void Validate_ShouldPass_WhenSlugIsExactly50Characters()
    {
        // Arrange
        var command = new CreateSubscriptionPlanCommand(
            Name: "Valid Name",
            Slug: new string('a', 50),
            MonthlyPriceInCents: 999,
            Currency: "USD",
            Description: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Pro Plan")] // Contains uppercase and space
    [InlineData("pro_plan")] // Contains underscore
    [InlineData("pro.plan")] // Contains period
    [InlineData("PRO-PLAN")] // Contains uppercase
    [InlineData("pro plan")] // Contains space
    public void Validate_ShouldFail_WhenSlugHasInvalidCharacters(string slug)
    {
        // Arrange
        var command = new CreateSubscriptionPlanCommand(
            Name: "Valid Name",
            Slug: slug,
            MonthlyPriceInCents: 999,
            Currency: "USD",
            Description: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == nameof(CreateSubscriptionPlanCommand.Slug) &&
            e.ErrorMessage.Contains("lowercase letters, numbers, and hyphens"));
    }

    [Theory]
    [InlineData("pro-plan")]
    [InlineData("pro-plan-123")]
    [InlineData("a-b-c")]
    [InlineData("123")]
    [InlineData("abc123")]
    [InlineData("plan1-tier2")]
    public void Validate_ShouldPass_WhenSlugHasValidCharacters(string slug)
    {
        // Arrange
        var command = new CreateSubscriptionPlanCommand(
            Name: "Valid Name",
            Slug: slug,
            MonthlyPriceInCents: 999,
            Currency: "USD",
            Description: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region MonthlyPriceInCents Validation

    [Fact]
    public void Validate_ShouldFail_WhenMonthlyPriceInCentsIsNegative()
    {
        // Arrange
        var command = new CreateSubscriptionPlanCommand(
            Name: "Valid Name",
            Slug: "valid-slug",
            MonthlyPriceInCents: -1,
            Currency: "USD",
            Description: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == nameof(CreateSubscriptionPlanCommand.MonthlyPriceInCents) &&
            e.ErrorMessage.Contains("greater than or equal to 0"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(999999)]
    public void Validate_ShouldPass_WhenMonthlyPriceInCentsIsZeroOrPositive(long price)
    {
        // Arrange
        var command = new CreateSubscriptionPlanCommand(
            Name: "Valid Name",
            Slug: "valid-slug",
            MonthlyPriceInCents: price,
            Currency: "USD",
            Description: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Currency Validation

    [Fact]
    public void Validate_ShouldFail_WhenCurrencyIsEmpty()
    {
        // Arrange
        var command = new CreateSubscriptionPlanCommand(
            Name: "Valid Name",
            Slug: "valid-slug",
            MonthlyPriceInCents: 999,
            Currency: "",
            Description: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == nameof(CreateSubscriptionPlanCommand.Currency) &&
            e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenCurrencyIsNull()
    {
        // Arrange
        var command = new CreateSubscriptionPlanCommand(
            Name: "Valid Name",
            Slug: "valid-slug",
            MonthlyPriceInCents: 999,
            Currency: null!,
            Description: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == nameof(CreateSubscriptionPlanCommand.Currency));
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("A")]
    public void Validate_ShouldFail_WhenCurrencyIsNotThreeCharacters(string currency)
    {
        // Arrange
        var command = new CreateSubscriptionPlanCommand(
            Name: "Valid Name",
            Slug: "valid-slug",
            MonthlyPriceInCents: 999,
            Currency: currency,
            Description: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == nameof(CreateSubscriptionPlanCommand.Currency) &&
            e.ErrorMessage.Contains("3 characters"));
    }

    [Theory]
    [InlineData("usd")] // lowercase
    [InlineData("UsD")] // mixed case
    [InlineData("123")] // numbers
    [InlineData("U$D")] // special chars
    public void Validate_ShouldFail_WhenCurrencyIsNotUppercaseLetters(string currency)
    {
        // Arrange
        var command = new CreateSubscriptionPlanCommand(
            Name: "Valid Name",
            Slug: "valid-slug",
            MonthlyPriceInCents: 999,
            Currency: currency,
            Description: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == nameof(CreateSubscriptionPlanCommand.Currency) &&
            e.ErrorMessage.Contains("uppercase letters only"));
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("GBP")]
    [InlineData("JPY")]
    [InlineData("CHF")]
    [InlineData("CAD")]
    [InlineData("AUD")]
    public void Validate_ShouldPass_WhenCurrencyIsValidISO4217(string currency)
    {
        // Arrange
        var command = new CreateSubscriptionPlanCommand(
            Name: "Valid Name",
            Slug: "valid-slug",
            MonthlyPriceInCents: 999,
            Currency: currency,
            Description: null
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Description Validation

    [Fact]
    public void Validate_ShouldFail_WhenDescriptionExceeds500Characters()
    {
        // Arrange
        var command = new CreateSubscriptionPlanCommand(
            Name: "Valid Name",
            Slug: "valid-slug",
            MonthlyPriceInCents: 999,
            Currency: "USD",
            Description: new string('a', 501)
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == nameof(CreateSubscriptionPlanCommand.Description) &&
            e.ErrorMessage.Contains("500 characters"));
    }

    [Fact]
    public void Validate_ShouldPass_WhenDescriptionIsExactly500Characters()
    {
        // Arrange
        var command = new CreateSubscriptionPlanCommand(
            Name: "Valid Name",
            Slug: "valid-slug",
            MonthlyPriceInCents: 999,
            Currency: "USD",
            Description: new string('a', 500)
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Multiple Validation Errors

    [Fact]
    public void Validate_ShouldReturnAllErrors_WhenMultipleFieldsInvalid()
    {
        // Arrange
        var command = new CreateSubscriptionPlanCommand(
            Name: "",
            Slug: "Invalid Slug!",
            MonthlyPriceInCents: -100,
            Currency: "invalid",
            Description: new string('a', 600)
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(4);
    }

    #endregion
}
