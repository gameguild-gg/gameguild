using FluentValidation.TestHelper;
using GameGuild.Users.Commands;
using Xunit;

namespace GameGuild.Users.UnitTests.Validators;

/// <summary>
/// Unit tests for CreateUserCommandValidator
/// </summary>
public class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator;

    public CreateUserCommandValidatorTests()
    {
        _validator = new CreateUserCommandValidator();
    }

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new CreateUserCommand(
            Email: "test@example.com",
            Name: "Test User",
            PhoneNumber: "+1234567890"
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WithInvalidEmail_ShouldHaveError(string invalidEmail)
    {
        // Arrange
        var command = new CreateUserCommand(
            Email: invalidEmail,
            Name: "Test User"
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_WithInvalidEmailFormat_ShouldHaveError()
    {
        // Arrange
        var command = new CreateUserCommand(
            Email: "not-an-email",
            Name: "Test User"
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email must be a valid email address.");
    }

    [Fact]
    public void Validate_WithEmailTooLong_ShouldHaveError()
    {
        // Arrange
        var longEmail = new string('a', 250) + "@test.com"; // Over 255 chars
        var command = new CreateUserCommand(
            Email: longEmail,
            Name: "Test User"
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email must not exceed 255 characters.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WithInvalidName_ShouldHaveError(string invalidName)
    {
        // Arrange
        var command = new CreateUserCommand(
            Email: "test@example.com",
            Name: invalidName
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WithNameTooShort_ShouldHaveError()
    {
        // Arrange
        var command = new CreateUserCommand(
            Email: "test@example.com",
            Name: "A" // Only 1 character
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name must be at least 2 characters.");
    }

    [Fact]
    public void Validate_WithNameTooLong_ShouldHaveError()
    {
        // Arrange
        var longName = new string('a', 101); // Over 100 chars
        var command = new CreateUserCommand(
            Email: "test@example.com",
            Name: longName
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name cannot exceed 100 characters.");
    }

    [Fact]
    public void Validate_WithPhoneNumberTooLong_ShouldHaveError()
    {
        // Arrange
        var longPhone = new string('1', 21); // Over 20 chars
        var command = new CreateUserCommand(
            Email: "test@example.com",
            Name: "Test User",
            PhoneNumber: longPhone
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber)
            .WithErrorMessage("Phone number cannot exceed 20 characters.");
    }

    [Fact]
    public void Validate_WithNullPhoneNumber_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateUserCommand(
            Email: "test@example.com",
            Name: "Test User",
            PhoneNumber: null
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [Fact]
    public void Validate_WithEmptyPhoneNumber_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateUserCommand(
            Email: "test@example.com",
            Name: "Test User",
            PhoneNumber: ""
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
    }
}
