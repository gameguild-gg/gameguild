using FluentAssertions;
using FluentValidation.TestHelper;
using GameGuild.Database;
using GameGuild.Modules.Credentials;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameGuild.Tests.Credentials.Unit.Validators;

/// <summary>
/// Unit tests for the DeactivateCredentialCommandValidator
/// Tests validation rules and business logic validation
/// </summary>
public class DeactivateCredentialCommandValidatorTests : IDisposable
{
    private readonly TestApplicationDbContext _context;
    private readonly DeactivateCredentialCommandValidator _validator;

    public DeactivateCredentialCommandValidatorTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new TestApplicationDbContext(options);
        _validator = new DeactivateCredentialCommandValidator(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenIdIsEmpty()
    {
        // Arrange
        var command = new DeactivateCredentialCommand(Guid.Empty);

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
        var command = new DeactivateCredentialCommand(nonExistentId);

        // Ensure no credential exists with this ID
        _context.Credentials.RemoveRange(_context.Credentials);
        await _context.SaveChangesAsync();

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Id)
              .WithErrorMessage("Credential not found");
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenCredentialIsAlreadyInactive()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new DeactivateCredentialCommand(credentialId);

        // Setup credential that is already inactive
        _context.Credentials.Add(new Credential
        {
            Id = credentialId,
            UserId = Guid.NewGuid(),
            Type = "password",
            Value = "hashed_value",
            IsActive = false // Already inactive
        });
        await _context.SaveChangesAsync();

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Id)
              .WithErrorMessage("Credential is already inactive");
    }

    [Fact]
    public async Task Validate_ShouldNotHaveError_WhenCredentialExistsAndIsActive()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new DeactivateCredentialCommand(credentialId);

        // Setup active credential
        _context.Credentials.Add(new Credential
        {
            Id = credentialId,
            UserId = Guid.NewGuid(),
            Type = "password",
            Value = "hashed_value",
            IsActive = true // Active credential
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
        var command = new DeactivateCredentialCommand(credentialId);

        // Setup valid active credential
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
    public async Task Validate_ShouldNotHaveError_ForDifferentActiveCredentialTypes(string credentialType)
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new DeactivateCredentialCommand(credentialId);

        // Setup active credential of different types
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
    public async Task Validate_ShouldHaveError_ForInvalidCredentialState()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new DeactivateCredentialCommand(credentialId);

        // Setup credential in an invalid state (e.g., expired and inactive)
        _context.Credentials.Add(new Credential
        {
            Id = credentialId,
            UserId = Guid.NewGuid(),
            Type = "password",
            Value = "hashed_value",
            IsActive = false,
            ExpiresAt = DateTime.UtcNow.AddDays(-10) // Expired and inactive
        });
        await _context.SaveChangesAsync();

        // Act & Assert
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Id)
              .WithErrorMessage("Credential is already inactive");
    }
}