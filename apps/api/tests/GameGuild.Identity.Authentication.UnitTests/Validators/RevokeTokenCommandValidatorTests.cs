using FluentValidation.TestHelper;
using GameGuild.Identity.Authentication;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Validators;

public class RevokeTokenCommandValidatorTests
{
    private readonly RevokeTokenCommandValidator _validator = new();

    [Fact]
    public void Should_HaveError_When_RefreshTokenIsEmpty()
    {
        var command = new RevokeTokenCommand { RefreshToken = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken);
    }

    [Fact]
    public void Should_NotHaveError_When_RefreshTokenIsProvided()
    {
        var command = new RevokeTokenCommand { RefreshToken = "some-token" };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.RefreshToken);
    }

    [Fact]
    public void Should_PassValidation_When_AllFieldsValid()
    {
        var command = new RevokeTokenCommand { RefreshToken = "valid-revoke-token" };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
