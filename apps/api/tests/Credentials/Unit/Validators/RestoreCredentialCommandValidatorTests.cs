using FluentAssertions;
using FluentValidation.TestHelper;
using GameGuild.Database;
using GameGuild.Modules.Credentials;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameGuild.Tests.Credentials.Unit.Validators;

/// <summary>
/// Unit tests for the RestoreCredentialCommandValidator
/// Tests validation rules for restoring soft-deleted credentials
/// </summary>
public class RestoreCredentialCommandValidatorTests : IDisposable
{
    private readonly TestApplicationDbContext _context;
    private readonly RestoreCredentialCommandValidator _validator;

    public RestoreCredentialCommandValidatorTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new TestApplicationDbContext(options);
        _validator = new RestoreCredentialCommandValidator(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenIdIsEmpty()
    {
        // Arrange
        var command = new RestoreCredentialCommand(Guid.Empty);

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
        var command = new RestoreCredentialCommand(nonExistentId);

        // Ensure no credential exists with this ID
        _context.Credentials.RemoveRange(_context.Credentials);
        await _context.SaveChangesAsync();

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Id)
              .WithErrorMessage("Credential not found");
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenCredentialIsNotDeleted()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new RestoreCredentialCommand(credentialId);

        // Setup credential that is not deleted (active credential)
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
        result.ShouldHaveValidationErrorFor(x => x.Id)
              .WithErrorMessage("Credential is not soft-deleted");
    }

    [Fact]
    public async Task Validate_ShouldNotHaveError_WhenCredentialIsDeleted()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new RestoreCredentialCommand(credentialId);

        // Setup soft-deleted credential
        _context.Credentials.Add(new Credential
        {
            Id = credentialId,
            UserId = Guid.NewGuid(),
            Type = "password",
            Value = "hashed_value",
            IsActive = false,
            DeletedAt = DateTime.UtcNow.AddDays(-1) // Soft deleted
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
        var command = new RestoreCredentialCommand(credentialId);

        // Setup valid soft-deleted credential
        _context.Credentials.Add(new Credential
        {
            Id = credentialId,
            UserId = Guid.NewGuid(),
            Type = "api_key",
            Value = "encrypted_key_value",
            IsActive = false,
            DeletedAt = DateTime.UtcNow.AddDays(-7),
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-7)
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
    public async Task Validate_ShouldNotHaveError_ForDifferentDeletedCredentialTypes(string credentialType)
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new RestoreCredentialCommand(credentialId);

        // Setup soft-deleted credential of different types
        _context.Credentials.Add(new Credential
        {
            Id = credentialId,
            UserId = Guid.NewGuid(),
            Type = credentialType,
            Value = "valid_value",
            IsActive = false,
            DeletedAt = DateTime.UtcNow.AddDays(-1)
        });
        await _context.SaveChangesAsync();

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_ShouldNotHaveError_ForExpiredDeletedCredential()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new RestoreCredentialCommand(credentialId);

        // Setup expired and soft-deleted credential
        _context.Credentials.Add(new Credential
        {
            Id = credentialId,
            UserId = Guid.NewGuid(),
            Type = "password",
            Value = "hashed_value",
            IsActive = false,
            ExpiresAt = DateTime.UtcNow.AddDays(-10), // Expired
            DeletedAt = DateTime.UtcNow.AddDays(-5) // Deleted after expiry
        });
        await _context.SaveChangesAsync();

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_Should_Handle_Validation_Failure_For_Active_Credential()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new RestoreCredentialCommand(credentialId);

        // Setup active credential (cannot be restored)
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
        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }
}