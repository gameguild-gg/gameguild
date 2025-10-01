using FluentAssertions;
using FluentValidation.TestHelper;
using GameGuild.Database;
using GameGuild.Modules.Credentials;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace GameGuild.Tests.Credentials.Unit.Validators;

/// <summary>
/// Unit tests for the CreateCredentialCommandValidator
/// Tests validation rules and business logic validation
/// </summary>
public class CreateCredentialCommandValidatorTests
{
    private readonly Mock<ApplicationDbContext> _mockContext;
    private readonly CreateCredentialCommandValidator _validator;

    public CreateCredentialCommandValidatorTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _mockContext = new Mock<ApplicationDbContext>(options);
        _validator = new CreateCredentialCommandValidator(_mockContext.Object);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenUserIdIsEmpty()
    {
        // Arrange
        var command = new CreateCredentialCommand { UserId = Guid.Empty };

        // Act & Assert
        _validator.TestValidate(command)
            .ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("User ID is required");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenTypeIsEmpty()
    {
        // Arrange
        var command = new CreateCredentialCommand
        {
            UserId = Guid.NewGuid(),
            Type = string.Empty
        };

        // Act & Assert
        _validator.TestValidate(command)
            .ShouldHaveValidationErrorFor(x => x.Type)
            .WithErrorMessage("Credential type is required");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenTypeIsTooLong()
    {
        // Arrange
        var command = new CreateCredentialCommand
        {
            UserId = Guid.NewGuid(),
            Type = new string('x', 51) // 51 characters, max is 50
        };

        // Act & Assert
        _validator.TestValidate(command)
            .ShouldHaveValidationErrorFor(x => x.Type)
            .WithErrorMessage("Credential type must be 50 characters or fewer");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenValueIsEmpty()
    {
        // Arrange
        var command = new CreateCredentialCommand
        {
            UserId = Guid.NewGuid(),
            Type = "password",
            Value = string.Empty
        };

        // Act & Assert
        _validator.TestValidate(command)
            .ShouldHaveValidationErrorFor(x => x.Value)
            .WithErrorMessage("Credential value is required");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenValueIsTooLong()
    {
        // Arrange
        var command = new CreateCredentialCommand
        {
            UserId = Guid.NewGuid(),
            Type = "password",
            Value = new string('x', 1001) // 1001 characters, max is 1000
        };

        // Act & Assert
        _validator.TestValidate(command)
            .ShouldHaveValidationErrorFor(x => x.Value)
            .WithErrorMessage("Credential value must be 1000 characters or fewer");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenMetadataIsTooLong()
    {
        // Arrange
        var command = new CreateCredentialCommand
        {
            UserId = Guid.NewGuid(),
            Type = "password",
            Value = "hashed_password",
            Metadata = new string('x', 2001) // 2001 characters, max is 2000
        };

        // Act & Assert
        _validator.TestValidate(command)
            .ShouldHaveValidationErrorFor(x => x.Metadata)
            .WithErrorMessage("Metadata must be 2000 characters or fewer");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenExpiresAtIsInPast()
    {
        // Arrange
        var command = new CreateCredentialCommand
        {
            UserId = Guid.NewGuid(),
            Type = "password",
            Value = "hashed_password",
            ExpiresAt = DateTime.UtcNow.AddDays(-1) // Yesterday
        };

        // Act & Assert
        _validator.TestValidate(command)
            .ShouldHaveValidationErrorFor(x => x.ExpiresAt)
            .WithErrorMessage("Expiration date must be in the future");
    }

    [Fact]
    public void Validate_ShouldNotHaveError_WhenExpiresAtIsInFuture()
    {
        // Arrange
        var command = new CreateCredentialCommand
        {
            UserId = Guid.NewGuid(),
            Type = "password",
            Value = "hashed_password",
            ExpiresAt = DateTime.UtcNow.AddDays(1) // Tomorrow
        };

        // Mock user exists check
        SetupUserExistsCheck(command.UserId, true);
        SetupUniqueCredentialCheck(command.UserId, command.Type, true);

        // Act & Assert
        _validator.TestValidate(command)
            .ShouldNotHaveValidationErrorFor(x => x.ExpiresAt);
    }

    [Fact]
    public void Validate_ShouldNotHaveError_WhenExpiresAtIsNull()
    {
        // Arrange
        var command = new CreateCredentialCommand
        {
            UserId = Guid.NewGuid(),
            Type = "password",
            Value = "hashed_password",
            ExpiresAt = null
        };

        // Mock user exists check
        SetupUserExistsCheck(command.UserId, true);
        SetupUniqueCredentialCheck(command.UserId, command.Type, true);

        // Act & Assert
        _validator.TestValidate(command)
            .ShouldNotHaveValidationErrorFor(x => x.ExpiresAt);
    }

    [Theory]
    [InlineData("password")]
    [InlineData("api_key")]
    [InlineData("oauth_token")]
    [InlineData("2fa_secret")]
    public void Validate_ShouldNotHaveError_ForValidCredentialTypes(string credentialType)
    {
        // Arrange
        var command = new CreateCredentialCommand
        {
            UserId = Guid.NewGuid(),
            Type = credentialType,
            Value = "valid_value"
        };

        // Mock user exists check
        SetupUserExistsCheck(command.UserId, true);
        SetupUniqueCredentialCheck(command.UserId, command.Type, true);

        // Act & Assert
        _validator.TestValidate(command)
            .ShouldNotHaveValidationErrorFor(x => x.Type);
    }

    [Fact]
    public void Validate_ShouldNotHaveError_ForValidMetadataLength()
    {
        // Arrange
        var validMetadata = new string('x', 1999); // Just under the limit
        var command = new CreateCredentialCommand
        {
            UserId = Guid.NewGuid(),
            Type = "password",
            Value = "hashed_password",
            Metadata = validMetadata
        };

        // Mock user exists check
        SetupUserExistsCheck(command.UserId, true);
        SetupUniqueCredentialCheck(command.UserId, command.Type, true);

        // Act & Assert
        _validator.TestValidate(command)
            .ShouldNotHaveValidationErrorFor(x => x.Metadata);
    }

    [Fact]
    public void Validate_ShouldPassAllValidations_ForValidCommand()
    {
        // Arrange
        var command = new CreateCredentialCommand
        {
            UserId = Guid.NewGuid(),
            Type = "password",
            Value = "hashed_password",
            Metadata = """{"algorithm": "bcrypt", "rounds": 12}""",
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            IsActive = true
        };

        // Mock user exists check
        SetupUserExistsCheck(command.UserId, true);
        SetupUniqueCredentialCheck(command.UserId, command.Type, true);

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    private void SetupUserExistsCheck(Guid userId, bool exists)
    {
        var mockUserSet = new Mock<DbSet<User>>();
        mockUserSet.Setup(s => s.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(exists);

        _mockContext.Setup(c => c.Users).Returns(mockUserSet.Object);
    }

    private void SetupUniqueCredentialCheck(Guid userId, string type, bool isUnique)
    {
        var mockCredentialSet = new Mock<DbSet<Credential>>();
        mockCredentialSet.Setup(s => s.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Credential, bool>>>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(!isUnique); // AnyAsync returns opposite of uniqueness

        _mockContext.Setup(c => c.Credentials).Returns(mockCredentialSet.Object);
    }
}