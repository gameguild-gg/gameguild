using FluentValidation.TestHelper;
using GameGuild.Modules.Authentication;
using Xunit;

namespace GameGuild.Tests.Authentication.Unit.Validators;

/// <summary>
/// Unit tests for the LocalSignInCommandValidator
/// Tests validation rules for local user sign-in commands
/// </summary>
public class LocalSignInCommandValidatorTests
{
    private readonly LocalSignInCommandValidator _validator;

    public LocalSignInCommandValidatorTests()
    {
        _validator = new LocalSignInCommandValidator();
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenEmailIsEmpty()
    {
        // Arrange
        var command = new LocalSignInCommand { Email = string.Empty, Password = "ValidPassword123!" };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Email is required");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenEmailIsNull()
    {
        // Arrange
        var command = new LocalSignInCommand { Email = null!, Password = "ValidPassword123!" };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Email cannot be null");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenEmailIsInvalid()
    {
        // Arrange
        var command = new LocalSignInCommand { Email = "invalid-email", Password = "ValidPassword123!" };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Email must be a valid email address");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenEmailIsTooLong()
    {
        // Arrange
        var longEmail = new string('a', 250) + "@test.com"; // 254+ characters
        var command = new LocalSignInCommand { Email = longEmail, Password = "ValidPassword123!" };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Email is too long");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenPasswordIsEmpty()
    {
        // Arrange
        var command = new LocalSignInCommand { Email = "test@example.com", Password = string.Empty };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Password is required");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenPasswordIsNull()
    {
        // Arrange
        var command = new LocalSignInCommand { Email = "test@example.com", Password = null! };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Password cannot be null");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenPasswordIsTooLong()
    {
        // Arrange
        var longPassword = new string('a', 129); // 129 characters
        var command = new LocalSignInCommand { Email = "test@example.com", Password = longPassword };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Password is too long");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenTenantIdIsEmpty()
    {
        // Arrange
        var command = new LocalSignInCommand
        {
            Email = "test@example.com",
            Password = "ValidPassword123!",
            TenantId = Guid.Empty
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.TenantId)
              .WithErrorMessage("Tenant ID must be a valid GUID when provided");
    }

    [Fact]
    public void Validate_ShouldPass_WhenAllFieldsAreValid()
    {
        // Arrange
        var command = new LocalSignInCommand
        {
            Email = "test@example.com",
            Password = "ValidPassword123!",
            TenantId = Guid.NewGuid()
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldPass_WhenTenantIdIsNull()
    {
        // Arrange
        var command = new LocalSignInCommand
        {
            Email = "test@example.com",
            Password = "ValidPassword123!",
            TenantId = null
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}