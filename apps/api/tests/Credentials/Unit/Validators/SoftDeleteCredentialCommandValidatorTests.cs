using FluentAssertions;
using FluentValidation.TestHelper;
using GameGuild.Database;
using GameGuild.Modules.Credentials;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameGuild.Tests.Credentials.Unit.Validators;

/// <summary>
/// Unit tests for the SoftDeleteCredentialCommandValidator
/// Tests validation rules for soft deletion of credentials
/// </summary>
public class SoftDeleteCredentialCommandValidatorTests : IDisposable
{
    private readonly TestApplicationDbContext _context;
    private readonly SoftDeleteCredentialCommandValidator _validator;

    public SoftDeleteCredentialCommandValidatorTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new TestApplicationDbContext(options);
        _validator = new SoftDeleteCredentialCommandValidator(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenIdIsEmpty()
    {
        // Arrange
        var command = new SoftDeleteCredentialCommand(Guid.Empty);

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
        var command = new SoftDeleteCredentialCommand(nonExistentId);

        // Ensure no credential exists with this ID
        _context.Credentials.RemoveRange(_context.Credentials);
        await _context.SaveChangesAsync();

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Id)
              .WithErrorMessage("Credential not found");
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenCredentialIsAlreadyDeleted()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new SoftDeleteCredentialCommand(credentialId);

        // Setup credential that is already soft deleted
        _context.Credentials.Add(new Credential
        {
            Id = credentialId,
            UserId = Guid.NewGuid(),
            Type = "password",
            Value = "hashed_value",
            IsActive = false,
            DeletedAt = DateTime.UtcNow.AddDays(-1) // Already soft deleted
        });
        await _context.SaveChangesAsync();

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Id)
              .WithErrorMessage("Credential is already soft-deleted");
    }

    [Fact]
    public async Task Validate_ShouldNotHaveError_WhenCredentialExistsAndIsNotDeleted()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new SoftDeleteCredentialCommand(credentialId);

        // Setup active credential that can be soft deleted
        _context.Credentials.Add(new Credential
        {
            Id = credentialId,
            UserId = Guid.NewGuid(),
            Type = "password",
            Value = "hashed_value",
            IsActive = true,
            DeletedAt = null // Not deleted
        });
        await _context.SaveChangesAsync();

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_ShouldNotHaveError_WhenCredentialIsInactiveButNotDeleted()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new SoftDeleteCredentialCommand(credentialId);

        // Setup inactive credential that can still be soft deleted
        _context.Credentials.Add(new Credential
        {
            Id = credentialId,
            UserId = Guid.NewGuid(),
            Type = "password",
            Value = "hashed_value",
            IsActive = false,
            DeletedAt = null // Not deleted, just inactive
        });
        await _context.SaveChangesAsync();

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_ShouldPassAllValidations_ForValidCommand()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new SoftDeleteCredentialCommand(credentialId);

        // Setup valid credential that can be soft deleted
        _context.Credentials.Add(new Credential
        {
            Id = credentialId,
            UserId = Guid.NewGuid(),
            Type = "api_key",
            Value = "encrypted_key_value",
            IsActive = true,
            DeletedAt = null,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        });
        await _context.SaveChangesAsync();

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("password")]
    [InlineData("api_key")]
    [InlineData("oauth_token")]
    [InlineData("2fa_secret")]
    public async Task Validate_ShouldNotHaveError_ForDifferentCredentialTypes(string credentialType)
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new SoftDeleteCredentialCommand(credentialId);

        // Setup credential of different types that can be soft deleted
        _context.Credentials.Add(new Credential
        {
            Id = credentialId,
            UserId = Guid.NewGuid(),
            Type = credentialType,
            Value = "valid_value",
            IsActive = true,
            DeletedAt = null
        });
        await _context.SaveChangesAsync();

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_ShouldNotHaveError_ForExpiredCredential()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new SoftDeleteCredentialCommand(credentialId);

        // Setup expired credential that can still be soft deleted
        _context.Credentials.Add(new Credential
        {
            Id = credentialId,
            UserId = Guid.NewGuid(),
            Type = "password",
            Value = "hashed_value",
            IsActive = false,
            ExpiresAt = DateTime.UtcNow.AddDays(-10), // Expired
            DeletedAt = null // Not deleted
        });
        await _context.SaveChangesAsync();

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_Should_Handle_Validation_Failure_For_Already_Deleted_Credential()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new SoftDeleteCredentialCommand(credentialId);

        // Setup credential that is already soft deleted
        _context.Credentials.Add(new Credential
        {
            Id = credentialId,
            UserId = Guid.NewGuid(),
            Type = "password",
            Value = "hashed_value",
            IsActive = false,
            DeletedAt = DateTime.UtcNow.AddDays(-5) // Already deleted
        });
        await _context.SaveChangesAsync();

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }
}