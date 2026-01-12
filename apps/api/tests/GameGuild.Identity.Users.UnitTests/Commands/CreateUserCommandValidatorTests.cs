using FluentValidation.TestHelper;
using GameGuild.Identity.Users;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveAnyValidationErrors()
    {
        // Arrange
        var command = new CreateUserCommand("test@example.com", "John Doe", "1234567890");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyEmail_ShouldHaveError()
    {
        // Arrange
        var command = new CreateUserCommand("", "John Doe", null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_WithInvalidEmailFormat_ShouldHaveError()
    {
        // Arrange
        var command = new CreateUserCommand("not-an-email", "John Doe", null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_WithTooLongEmail_ShouldHaveError()
    {
        // Arrange
        var longEmail = new string('a', 250) + "@test.com"; // 259 characters
        var command = new CreateUserCommand(longEmail, "John Doe", null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_WithEmptyName_ShouldHaveError()
    {
        // Arrange
        var command = new CreateUserCommand("test@example.com", "", null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WithTooShortName_ShouldHaveError()
    {
        // Arrange
        var command = new CreateUserCommand("test@example.com", "A", null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WithTooLongName_ShouldHaveError()
    {
        // Arrange
        var longName = new string('A', 101);
        var command = new CreateUserCommand("test@example.com", longName, null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WithTooLongPhoneNumber_ShouldHaveError()
    {
        // Arrange
        var longPhone = new string('1', 21);
        var command = new CreateUserCommand("test@example.com", "John Doe", longPhone);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [Fact]
    public void Validate_WithNullPhoneNumber_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateUserCommand("test@example.com", "John Doe", null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [Fact]
    public void Validate_WithValidPhoneNumber_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateUserCommand("test@example.com", "John Doe", "+1234567890");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
    }
}
