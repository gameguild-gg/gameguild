using FluentAssertions;
using FluentValidation.TestHelper;
using GameGuild.Modules.Authentication;
using Xunit;

namespace GameGuild.Tests.Authentication.Unit.Validators;

/// <summary>
/// Unit tests for the RefreshTokenCommandValidator
/// Tests validation rules for refresh token commands
/// </summary>
public class RefreshTokenCommandValidatorTests
{
    private readonly RefreshTokenCommandValidator _validator;

    public RefreshTokenCommandValidatorTests()
    {
        _validator = new RefreshTokenCommandValidator();
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenRefreshTokenIsEmpty()
    {
        // Arrange
        var command = new RefreshTokenCommand { RefreshToken = string.Empty };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken)
              .WithErrorMessage("Refresh token is required");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenRefreshTokenIsNull()
    {
        // Arrange
        var command = new RefreshTokenCommand { RefreshToken = null! };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken)
              .WithErrorMessage("Refresh token cannot be null");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenRefreshTokenIsTooShort()
    {
        // Arrange
        var command = new RefreshTokenCommand { RefreshToken = "short" };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken)
              .WithErrorMessage("Refresh token appears to be invalid");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenTenantIdIsEmpty()
    {
        // Arrange
        var command = new RefreshTokenCommand
        {
            RefreshToken = "valid-refresh-token-12345",
            TenantId = Guid.Empty
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.TenantId)
              .WithErrorMessage("Tenant ID must be a valid GUID when provided");
    }

    [Fact]
    public void Validate_ShouldPass_WhenRefreshTokenIsValid()
    {
        // Arrange
        var command = new RefreshTokenCommand
        {
            RefreshToken = "valid-refresh-token-12345",
            TenantId = null
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldPass_WhenAllFieldsAreValid()
    {
        // Arrange
        var command = new RefreshTokenCommand
        {
            RefreshToken = "valid-refresh-token-12345",
            TenantId = Guid.NewGuid()
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}