using FluentAssertions;
using GameGuild.Database;
using GameGuild.Modules.Credentials;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameGuild.Tests.Credentials.Unit.Validators;

/// <summary>
/// Unit tests for the ActivateCredentialCommandValidator
/// Tests validation logic for credential activation commands
/// </summary>
public class ActivateCredentialCommandValidatorTests : IAsyncDisposable
{
    private readonly TestApplicationDbContext _context;
    private readonly ActivateCredentialCommandValidator _validator;

    public ActivateCredentialCommandValidatorTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestApplicationDbContext(options);
        _validator = new ActivateCredentialCommandValidator(_context);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task Should_Have_Valid_Validation_Rules_For_Existing_Inactive_Credential()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var credential = new Credential
        {
            Id = credentialId,
            Type = "api_key",
            UserId = Guid.NewGuid(),
            IsActive = false
        };

        _context.Credentials.Add(credential);
        await _context.SaveChangesAsync();

        var command = new ActivateCredentialCommand(credentialId);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Have_Error_When_Id_Is_Empty()
    {
        // Arrange
        var command = new ActivateCredentialCommand(Guid.Empty);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(ActivateCredentialCommand.Id));
    }

    [Fact]
    public async Task Should_Fail_Validation_When_Credential_Does_Not_Exist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var command = new ActivateCredentialCommand(nonExistentId);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.ErrorMessage.Contains("not found"));
    }

    [Fact]
    public async Task Should_Fail_Validation_When_Credential_Is_Already_Active()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var credential = new Credential
        {
            Id = credentialId,
            Type = "password",
            UserId = Guid.NewGuid(),
            IsActive = true // Already active
        };

        _context.Credentials.Add(credential);
        await _context.SaveChangesAsync();

        var command = new ActivateCredentialCommand(credentialId);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.ErrorMessage.Contains("already active"));
    }

    [Fact]
    public void Validator_Should_Be_Instantiated_Successfully()
    {
        // Act & Assert
        _validator.Should().NotBeNull();
        _validator.Should().BeOfType<ActivateCredentialCommandValidator>();
    }
}