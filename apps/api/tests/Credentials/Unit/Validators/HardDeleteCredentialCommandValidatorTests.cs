using FluentAssertions;
using FluentValidation.TestHelper;
using GameGuild.Database;
using GameGuild.Modules.Credentials;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameGuild.Tests.Credentials.Unit.Validators;

/// <summary>
/// Unit tests for the HardDeleteCredentialCommandValidator
/// Tests validation rules for hard deletion of credentials
/// </summary>
public class HardDeleteCredentialCommandValidatorTests : IDisposable
{
    private readonly TestApplicationDbContext _context;
    private readonly HardDeleteCredentialCommandValidator _validator;

    public HardDeleteCredentialCommandValidatorTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new TestApplicationDbContext(options);
        _validator = new HardDeleteCredentialCommandValidator(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenIdIsEmpty()
    {
        // Arrange
        var command = new HardDeleteCredentialCommand(Guid.Empty);

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
        var command = new HardDeleteCredentialCommand(nonExistentId);

        // Ensure no credential exists with this ID
        _context.Credentials.RemoveRange(_context.Credentials);
        await _context.SaveChangesAsync();

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Id)
              .WithErrorMessage("Credential not found");
    }

    [Fact]
    public async Task Validate_ShouldNotHaveError_WhenCredentialExists()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new HardDeleteCredentialCommand(credentialId);

        // Setup existing credential
        _context.Credentials.Add(new Credential
        {
            Id = credentialId,
            UserId = Guid.NewGuid(),
            Type = "password",
            Value = "hashed_value",
            IsActive = true
        });
        await _context.SaveChangesAsync();

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_ShouldNotHaveError_WhenCredentialIsInactive()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new HardDeleteCredentialCommand(credentialId);

        // Setup inactive credential - hard delete should work on inactive credentials too
        _context.Credentials.Add(new Credential
        {
            Id = credentialId,
            UserId = Guid.NewGuid(),
            Type = "password",
            Value = "hashed_value",
            IsActive = false
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
        var command = new HardDeleteCredentialCommand(credentialId);

        // Setup valid credential
        _context.Credentials.Add(new Credential
        {
            Id = credentialId,
            UserId = Guid.NewGuid(),
            Type = "api_key",
            Value = "encrypted_key_value",
            IsActive = true,
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
        var command = new HardDeleteCredentialCommand(credentialId);

        // Setup credential of different types
        _context.Credentials.Add(new Credential
        {
            Id = credentialId,
            UserId = Guid.NewGuid(),
            Type = credentialType,
            Value = "valid_value",
            IsActive = true
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
        var command = new HardDeleteCredentialCommand(credentialId);

        // Setup expired credential - hard delete should work on expired credentials
        _context.Credentials.Add(new Credential
        {
            Id = credentialId,
            UserId = Guid.NewGuid(),
            Type = "password",
            Value = "hashed_value",
            IsActive = false,
            ExpiresAt = DateTime.UtcNow.AddDays(-10) // Expired
        });
        await _context.SaveChangesAsync();

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_Should_Handle_Validation_Failure_For_Missing_Credential()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var command = new HardDeleteCredentialCommand(nonExistentId);

        // Ensure database is empty
        _context.Credentials.RemoveRange(_context.Credentials);
        await _context.SaveChangesAsync();

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }
}