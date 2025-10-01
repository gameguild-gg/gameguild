using FluentAssertions;
using FluentValidation.TestHelper;
using GameGuild.Modules.Authentication;
using Xunit;

namespace GameGuild.Tests.Authentication.Unit.Validators;

/// <summary>
/// Unit tests for the RevokeTokenCommandValidator
/// Tests validation rules for revoke token commands
/// </summary>
public class RevokeTokenCommandValidatorTests
{
    private readonly RevokeTokenCommandValidator _validator;

    public RevokeTokenCommandValidatorTests()
    {
        _validator = new RevokeTokenCommandValidator();
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenRefreshTokenIsEmpty()
    {
        // Arrange
        var command = new RevokeTokenCommand { RefreshToken = string.Empty };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken)
              .WithErrorMessage("Refresh token is required");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenRefreshTokenIsNull()
    {
        // Arrange
        var command = new RevokeTokenCommand { RefreshToken = null! };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken)
              .WithErrorMessage("Refresh token cannot be null");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenRefreshTokenIsTooShort()
    {
        // Arrange
        var command = new RevokeTokenCommand { RefreshToken = "short" };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken)
              .WithErrorMessage("Refresh token appears to be invalid");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenIpAddressIsTooLong()
    {
        // Arrange
        var longIpAddress = new string('1', 46); // 46 characters
        var command = new RevokeTokenCommand
        {
            RefreshToken = "valid-refresh-token-12345",
            IpAddress = longIpAddress
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.IpAddress)
              .WithErrorMessage("IP address is too long");
    }

    [Fact]
    public void Validate_ShouldPass_WhenRefreshTokenIsValid()
    {
        // Arrange
        var command = new RevokeTokenCommand
        {
            RefreshToken = "valid-refresh-token-12345"
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldPass_WhenIpAddressIsNull()
    {
        // Arrange
        var command = new RevokeTokenCommand
        {
            RefreshToken = "valid-refresh-token-12345",
            IpAddress = null
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldPass_WhenIpAddressIsEmpty()
    {
        // Arrange
        var command = new RevokeTokenCommand
        {
            RefreshToken = "valid-refresh-token-12345",
            IpAddress = ""
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("192.168.1.1")] // IPv4
    [InlineData("2001:0db8:85a3:0000:0000:8a2e:0370:7334")] // IPv6
    [InlineData("::1")] // IPv6 loopback
    public void Validate_ShouldPass_WhenIpAddressIsValid(string ipAddress)
    {
        // Arrange
        var command = new RevokeTokenCommand
        {
            RefreshToken = "valid-refresh-token-12345",
            IpAddress = ipAddress
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}