using System.Linq.Expressions;
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
public class CreateCredentialCommandValidatorTests : IDisposable
{
    private readonly TestApplicationDbContext _context;
    private readonly CreateCredentialCommandValidator _validator;

    public CreateCredentialCommandValidatorTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new TestApplicationDbContext(options);
        _validator = new CreateCredentialCommandValidator(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenUserIdIsEmpty()
    {
        // Arrange
        var command = new CreateCredentialCommand { UserId = Guid.Empty };

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.UserId)
              .WithErrorMessage("User ID is required");
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenTypeIsEmpty()
    {
        // Arrange
        var command = new CreateCredentialCommand
        {
            UserId = Guid.NewGuid(),
            Type = string.Empty
        };

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Type)
              .WithErrorMessage("Credential type is required");
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenTypeIsTooLong()
    {
        // Arrange
        var command = new CreateCredentialCommand
        {
            UserId = Guid.NewGuid(),
            Type = new string('x', 51) // 51 characters, max is 50
        };

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Type)
              .WithErrorMessage("Credential type must be 50 characters or fewer");
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenValueIsEmpty()
    {
        // Arrange
        var command = new CreateCredentialCommand
        {
            UserId = Guid.NewGuid(),
            Type = "password",
            Value = string.Empty
        };

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Value)
              .WithErrorMessage("Credential value is required");
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenValueIsTooLong()
    {
        // Arrange
        var command = new CreateCredentialCommand
        {
            UserId = Guid.NewGuid(),
            Type = "password",
            Value = new string('x', 1001) // 1001 characters, max is 1000
        };

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Value)
              .WithErrorMessage("Credential value must be 1000 characters or fewer");
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenMetadataIsTooLong()
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
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Metadata)
              .WithErrorMessage("Metadata must be 2000 characters or fewer");
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenExpiresAtIsInPast()
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
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.ExpiresAt)
              .WithErrorMessage("Expiration date must be in the future");
    }

    [Fact]
    public async Task Validate_ShouldNotHaveError_WhenExpiresAtIsInFuture()
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
        await SetupUserExistsCheck(command.UserId, true);
        await SetupUniqueCredentialCheck(command.UserId, command.Type, true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ExpiresAt);
    }

    [Fact]
    public async Task Validate_ShouldNotHaveError_WhenExpiresAtIsNull()
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
        await SetupUserExistsCheck(command.UserId, true);
        await SetupUniqueCredentialCheck(command.UserId, command.Type, true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ExpiresAt);
    }

    [Theory]
    [InlineData("password")]
    [InlineData("api_key")]
    [InlineData("oauth_token")]
    [InlineData("2fa_secret")]
    public async Task Validate_ShouldNotHaveError_ForValidCredentialTypes(string credentialType)
    {
        // Arrange
        var command = new CreateCredentialCommand
        {
            UserId = Guid.NewGuid(),
            Type = credentialType,
            Value = "valid_value"
        };

        // Mock user exists check
        await SetupUserExistsCheck(command.UserId, true);
        await SetupUniqueCredentialCheck(command.UserId, command.Type, true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Type);
    }

    [Fact]
    public async Task Validate_ShouldNotHaveError_ForValidMetadataLength()
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
        await SetupUserExistsCheck(command.UserId, true);
        await SetupUniqueCredentialCheck(command.UserId, command.Type, true);

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Metadata);
    }

    [Fact]
    public async Task Validate_ShouldPassAllValidations_ForValidCommand()
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
        await SetupUserExistsCheck(command.UserId, true);
        await SetupUniqueCredentialCheck(command.UserId, command.Type, true);

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    private async Task SetupUserExistsCheck(Guid userId, bool exists)
    {
        // Clear existing data
        _context.Users.RemoveRange(_context.Users);

        if (exists)
        {
            _context.Users.Add(new User { Id = userId });
            await _context.SaveChangesAsync();
        }
    }

    private async Task SetupUniqueCredentialCheck(Guid userId, string type, bool isUnique)
    {
        // Clear existing credentials
        _context.Credentials.RemoveRange(_context.Credentials);

        if (!isUnique)
        {
            // Add existing credential to make it non-unique
            _context.Credentials.Add(new Credential { UserId = userId, Type = type, Value = "existing" });
            await _context.SaveChangesAsync();
        }
    }
}