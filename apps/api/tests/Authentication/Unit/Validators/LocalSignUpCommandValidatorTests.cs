using FluentAssertions;
using FluentValidation.TestHelper;
using GameGuild.Modules.Authentication;
using Xunit;

namespace GameGuild.Tests.Authentication.Unit.Validators;

/// <summary>
/// Unit tests for the LocalSignUpCommandValidator
/// Tests validation rules for local user sign-up commands
/// </summary>
public class LocalSignUpCommandValidatorTests
{
    private readonly LocalSignUpCommandValidator _validator;

    public LocalSignUpCommandValidatorTests()
    {
        _validator = new LocalSignUpCommandValidator();
    }

    #region Email Tests

    [Fact]
    public void Validate_ShouldHaveError_WhenEmailIsEmpty()
    {
        // Arrange
        var command = new LocalSignUpCommand
        {
            Email = string.Empty,
            Password = "ValidPassword123!",
            Username = "testuser"
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Email is required");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenEmailIsNull()
    {
        // Arrange
        var command = new LocalSignUpCommand
        {
            Email = null!,
            Password = "ValidPassword123!",
            Username = "testuser"
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Email cannot be null");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenEmailIsInvalid()
    {
        // Arrange
        var command = new LocalSignUpCommand
        {
            Email = "invalid-email",
            Password = "ValidPassword123!",
            Username = "testuser"
        };

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
        var command = new LocalSignUpCommand
        {
            Email = longEmail,
            Password = "ValidPassword123!",
            Username = "testuser"
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Email is too long");
    }

    #endregion

    #region Password Tests

    [Fact]
    public void Validate_ShouldHaveError_WhenPasswordIsEmpty()
    {
        // Arrange
        var command = new LocalSignUpCommand
        {
            Email = "test@example.com",
            Password = string.Empty,
            Username = "testuser"
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Password is required");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenPasswordIsNull()
    {
        // Arrange
        var command = new LocalSignUpCommand
        {
            Email = "test@example.com",
            Password = null!,
            Username = "testuser"
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Password cannot be null");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenPasswordIsTooShort()
    {
        // Arrange
        var command = new LocalSignUpCommand
        {
            Email = "test@example.com",
            Password = "Short1!",
            Username = "testuser"
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Password must be at least 8 characters long");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenPasswordIsTooLong()
    {
        // Arrange
        var longPassword = new string('A', 120) + "1!bcdefg"; // 129 characters
        var command = new LocalSignUpCommand
        {
            Email = "test@example.com",
            Password = longPassword,
            Username = "testuser"
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Password is too long");
    }

    [Theory]
    [InlineData("password123!")] // No uppercase
    [InlineData("PASSWORD123!")] // No lowercase
    [InlineData("Password!")] // No digit
    [InlineData("Password123")] // No special character
    public void Validate_ShouldHaveError_WhenPasswordDoesNotMeetComplexityRequirements(string password)
    {
        // Arrange
        var command = new LocalSignUpCommand
        {
            Email = "test@example.com",
            Password = password,
            Username = "testuser"
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character");
    }

    #endregion

    #region Username Tests

    [Fact]
    public void Validate_ShouldHaveError_WhenUsernameIsEmpty()
    {
        // Arrange
        var command = new LocalSignUpCommand
        {
            Email = "test@example.com",
            Password = "ValidPassword123!",
            Username = string.Empty
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Username)
              .WithErrorMessage("Username is required");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenUsernameIsNull()
    {
        // Arrange
        var command = new LocalSignUpCommand
        {
            Email = "test@example.com",
            Password = "ValidPassword123!",
            Username = null!
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Username)
              .WithErrorMessage("Username cannot be null");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenUsernameIsTooShort()
    {
        // Arrange
        var command = new LocalSignUpCommand
        {
            Email = "test@example.com",
            Password = "ValidPassword123!",
            Username = "ab"
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Username)
              .WithErrorMessage("Username must be at least 3 characters long");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenUsernameIsTooLong()
    {
        // Arrange
        var longUsername = new string('a', 51); // 51 characters
        var command = new LocalSignUpCommand
        {
            Email = "test@example.com",
            Password = "ValidPassword123!",
            Username = longUsername
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Username)
              .WithErrorMessage("Username is too long");
    }

    [Theory]
    [InlineData("user@name")] // Contains @
    [InlineData("user name")] // Contains space
    [InlineData("user#name")] // Contains #
    [InlineData("user$name")] // Contains $
    public void Validate_ShouldHaveError_WhenUsernameContainsInvalidCharacters(string username)
    {
        // Arrange
        var command = new LocalSignUpCommand
        {
            Email = "test@example.com",
            Password = "ValidPassword123!",
            Username = username
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Username)
              .WithErrorMessage("Username can only contain letters, numbers, dots, hyphens, and underscores");
    }

    [Theory]
    [InlineData("user.name")]
    [InlineData("user_name")]
    [InlineData("user-name")]
    [InlineData("user123")]
    [InlineData("User123")]
    public void Validate_ShouldPass_WhenUsernameContainsValidCharacters(string username)
    {
        // Arrange
        var command = new LocalSignUpCommand
        {
            Email = "test@example.com",
            Password = "ValidPassword123!",
            Username = username
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Username);
    }

    #endregion

    #region TenantId Tests

    [Fact]
    public void Validate_ShouldHaveError_WhenTenantIdIsEmpty()
    {
        // Arrange
        var command = new LocalSignUpCommand
        {
            Email = "test@example.com",
            Password = "ValidPassword123!",
            Username = "testuser",
            TenantId = Guid.Empty
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.TenantId)
              .WithErrorMessage("Tenant ID must be a valid GUID when provided");
    }

    [Fact]
    public void Validate_ShouldPass_WhenTenantIdIsNull()
    {
        // Arrange
        var command = new LocalSignUpCommand
        {
            Email = "test@example.com",
            Password = "ValidPassword123!",
            Username = "testuser",
            TenantId = null
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldPass_WhenTenantIdIsValid()
    {
        // Arrange
        var command = new LocalSignUpCommand
        {
            Email = "test@example.com",
            Password = "ValidPassword123!",
            Username = "testuser",
            TenantId = Guid.NewGuid()
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    [Fact]
    public void Validate_ShouldPass_WhenAllFieldsAreValid()
    {
        // Arrange
        var command = new LocalSignUpCommand
        {
            Email = "test@example.com",
            Password = "ValidPassword123!",
            Username = "testuser",
            TenantId = Guid.NewGuid()
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}