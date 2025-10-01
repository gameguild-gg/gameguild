using FluentAssertions;
using FluentValidation.TestHelper;
using GameGuild.Database;
using GameGuild.Modules.Credentials;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameGuild.Tests.Credentials.Unit.Validators;

/// <summary>
/// Unit tests for the UpdateCredentialCommandValidator
/// Tests validation rules and business logic validation for updating credentials
/// </summary>
public class UpdateCredentialCommandValidatorTests : IDisposable
{
    private readonly TestApplicationDbContext _context;
    private readonly UpdateCredentialCommandValidator _validator;

    public UpdateCredentialCommandValidatorTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new TestApplicationDbContext(options);
        _validator = new UpdateCredentialCommandValidator(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenIdIsEmpty()
    {
        // Arrange
        var command = new UpdateCredentialCommand
        {
            Id = Guid.Empty,
            Value = "new_value"
        };

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Id)
              .WithErrorMessage("Credential ID is required");
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenCredentialDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var command = new UpdateCredentialCommand
        {
            Id = nonExistentId,
            Value = "new_value"
        };

        // Ensure no credential exists with this ID
        _context.Credentials.RemoveRange(_context.Credentials);
        await _context.SaveChangesAsync();

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Id)
              .WithErrorMessage("Credential not found");
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenValueIsEmpty()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new UpdateCredentialCommand
        {
            Id = credentialId,
            Value = string.Empty
        };

        // Setup existing credential
        _context.Credentials.Add(new Credential
        {
            Id = credentialId,
            UserId = Guid.NewGuid(),
            Type = "password",
            Value = "existing_value",
            IsActive = true
        });
        await _context.SaveChangesAsync();

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Value)
              .WithErrorMessage("Credential value is required");
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenValueIsTooLong()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new UpdateCredentialCommand
        {
            Id = credentialId,
            Value = new string('x', 1001) // 1001 characters, max is 1000
        };

        // Setup existing credential
        _context.Credentials.Add(new Credential
        {
            Id = credentialId,
            UserId = Guid.NewGuid(),
            Type = "password",
            Value = "existing_value",
            IsActive = true
        });
        await _context.SaveChangesAsync();

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Value)
              .WithErrorMessage("Credential value must be 1000 characters or fewer");
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenMetadataIsTooLong()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new UpdateCredentialCommand
        {
            Id = credentialId,
            Value = "new_value",
            Metadata = new string('x', 2001) // 2001 characters, max is 2000
        };

        // Setup existing credential
        _context.Credentials.Add(new Credential
        {
            Id = credentialId,
            UserId = Guid.NewGuid(),
            Type = "password",
            Value = "existing_value",
            IsActive = true
        });
        await _context.SaveChangesAsync();

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Metadata)
              .WithErrorMessage("Metadata must be 2000 characters or fewer");
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenExpiresAtIsInPast()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new UpdateCredentialCommand
        {
            Id = credentialId,
            Value = "new_value",
            ExpiresAt = DateTime.UtcNow.AddDays(-1) // Yesterday
        };

        // Setup existing credential
        _context.Credentials.Add(new Credential
        {
            Id = credentialId,
            UserId = Guid.NewGuid(),
            Type = "password",
            Value = "existing_value",
            IsActive = true
        });
        await _context.SaveChangesAsync();

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.ExpiresAt)
              .WithErrorMessage("Expiration date must be in the future");
    }

    [Fact]
    public async Task Validate_ShouldNotHaveError_WhenExpiresAtIsInFuture()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new UpdateCredentialCommand
        {
            Id = credentialId,
            Value = "new_value",
            ExpiresAt = DateTime.UtcNow.AddDays(1) // Tomorrow
        };

        // Setup existing credential
        _context.Credentials.Add(new Credential
        {
            Id = credentialId,
            UserId = Guid.NewGuid(),
            Type = "password",
            Value = "existing_value",
            IsActive = true
        });
        await _context.SaveChangesAsync();

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveValidationErrorFor(x => x.ExpiresAt);
    }

    [Fact]
    public async Task Validate_ShouldNotHaveError_WhenExpiresAtIsNull()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new UpdateCredentialCommand
        {
            Id = credentialId,
            Value = "new_value",
            ExpiresAt = null
        };

        // Setup existing credential
        _context.Credentials.Add(new Credential
        {
            Id = credentialId,
            UserId = Guid.NewGuid(),
            Type = "password",
            Value = "existing_value",
            IsActive = true
        });
        await _context.SaveChangesAsync();

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveValidationErrorFor(x => x.ExpiresAt);
    }

    [Fact]
    public async Task Validate_ShouldPassAllValidations_ForValidCommand()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new UpdateCredentialCommand
        {
            Id = credentialId,
            Type = "Login",
            Value = "new_hashed_password",
            Metadata = """{"algorithm": "bcrypt", "rounds": 12}""",
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            IsActive = true
        };

        // Setup existing credential
        _context.Credentials.Add(new Credential
        {
            Id = credentialId,
            UserId = Guid.NewGuid(),
            Type = "password",
            Value = "old_value",
            IsActive = true
        });
        await _context.SaveChangesAsync();

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_ShouldNotHaveError_ForValidMetadataLength()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var validMetadata = new string('x', 1999); // Just under the limit
        var command = new UpdateCredentialCommand
        {
            Id = credentialId,
            Value = "new_value",
            Metadata = validMetadata
        };

        // Setup existing credential
        _context.Credentials.Add(new Credential
        {
            Id = credentialId,
            UserId = Guid.NewGuid(),
            Type = "password",
            Value = "existing_value",
            IsActive = true
        });
        await _context.SaveChangesAsync();

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Metadata);
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenCredentialIsDeleted()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new UpdateCredentialCommand
        {
            Id = credentialId,
            Type = "Login",
            Value = "new_value"
        };

        // Setup deleted credential
        _context.Credentials.Add(new Credential
        {
            Id = credentialId,
            UserId = Guid.NewGuid(),
            Type = "password",
            Value = "existing_value",
            IsActive = false,
            DeletedAt = DateTime.UtcNow.AddDays(-1) // Soft deleted
        });
        await _context.SaveChangesAsync();

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Id)
              .WithErrorMessage("Credential not found");
    }

    [Fact]
    public async Task Validate_ShouldNotHaveError_ForInactiveButNotDeletedCredential()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new UpdateCredentialCommand
        {
            Id = credentialId,
            Type = "Login",
            Value = "new_value"
        };

        // Setup inactive but not deleted credential
        _context.Credentials.Add(new Credential
        {
            Id = credentialId,
            UserId = Guid.NewGuid(),
            Type = "password",
            Value = "existing_value",
            IsActive = false,
            DeletedAt = null // Not deleted, just inactive
        });
        await _context.SaveChangesAsync();

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}