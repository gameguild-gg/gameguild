using FluentValidation.TestHelper;
using GameGuild.Modules.Users;
using Moq;
using Xunit;

namespace GameGuild.Tests.Users.Unit.Validators;

/// <summary>
/// Unit tests for CreateUserCommandValidator
/// Tests validation rules for user creation
/// </summary>
public class CreateUserCommandValidatorTests
{
    private readonly Mock<IUserService> _mockUserService;
    private readonly CreateUserCommandValidator _validator;

    public CreateUserCommandValidatorTests()
    {
        _mockUserService = new Mock<IUserService>();
        _validator = new CreateUserCommandValidator(_mockUserService.Object);
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenEmailIsEmpty()
    {
        // Arrange
        var command = new CreateUserCommand { Email = string.Empty };

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Email is required");
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenEmailIsInvalid()
    {
        // Arrange
        var command = new CreateUserCommand { Email = "invalid-email" };

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Invalid email format");
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenEmailIsTooLong()
    {
        // Arrange
        var command = new CreateUserCommand { Email = new string('x', 256) + "@test.com" };

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenEmailAlreadyExists()
    {
        // Arrange
        var command = new CreateUserCommand { Email = "existing@test.com" };
        _mockUserService.Setup(s => s.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(new User { Email = "existing@test.com" });

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Email address is already in use");
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenGivenNameIsTooLong()
    {
        // Arrange
        var command = new CreateUserCommand
        {
            Email = "test@test.com",
            GivenName = new string('x', 101)
        };

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.GivenName)
              .WithErrorMessage("Given name must be between 1 and 100 characters");
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenGivenNameContainsInvalidCharacters()
    {
        // Arrange
        var command = new CreateUserCommand
        {
            Email = "test@test.com",
            GivenName = "John123"
        };

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.GivenName)
              .WithErrorMessage("Given name can only contain letters, spaces, hyphens, apostrophes, and periods");
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenFamilyNameIsTooLong()
    {
        // Arrange
        var command = new CreateUserCommand
        {
            Email = "test@test.com",
            FamilyName = new string('x', 101)
        };

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.FamilyName)
              .WithErrorMessage("Family name must be between 1 and 100 characters");
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenFamilyNameContainsInvalidCharacters()
    {
        // Arrange
        var command = new CreateUserCommand
        {
            Email = "test@test.com",
            FamilyName = "Doe123"
        };

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.FamilyName)
              .WithErrorMessage("Family name can only contain letters, spaces, hyphens, apostrophes, and periods");
    }

    [Fact]
    public async Task Validate_ShouldPass_WhenAllFieldsAreValid()
    {
        // Arrange
        var command = new CreateUserCommand
        {
            Email = "valid@test.com",
            GivenName = "John",
            FamilyName = "Doe",
            IsActive = true
        };
        _mockUserService.Setup(s => s.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_ShouldPass_WhenOptionalFieldsAreNull()
    {
        // Arrange
        var command = new CreateUserCommand
        {
            Email = "valid@test.com",
            GivenName = null,
            FamilyName = null
        };
        _mockUserService.Setup(s => s.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
