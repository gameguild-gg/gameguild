using FluentValidation.TestHelper;
using GameGuild.Identity.Authentication;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Validators;

public class RefreshTokenCommandValidatorTests
{
    private readonly RefreshTokenCommandValidator _validator = new();

    [Fact]
    public void Should_HaveError_When_RefreshTokenIsEmpty()
    {
        var command = new RefreshTokenCommand { RefreshToken = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken);
    }

    [Fact]
    public void Should_NotHaveError_When_RefreshTokenIsProvided()
    {
        var command = new RefreshTokenCommand { RefreshToken = "valid-token-value" };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.RefreshToken);
    }

    [Fact]
    public void Should_HaveError_When_TenantIdIsEmptyGuid()
    {
        var command = new RefreshTokenCommand { RefreshToken = "token", TenantId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Should_NotHaveError_When_TenantIdIsNull()
    {
        var command = new RefreshTokenCommand { RefreshToken = "token", TenantId = null };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Should_NotHaveError_When_TenantIdIsValid()
    {
        var command = new RefreshTokenCommand { RefreshToken = "token", TenantId = Guid.NewGuid() };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Should_PassValidation_When_AllFieldsValid()
    {
        var command = new RefreshTokenCommand { RefreshToken = "valid-token", TenantId = Guid.NewGuid() };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
