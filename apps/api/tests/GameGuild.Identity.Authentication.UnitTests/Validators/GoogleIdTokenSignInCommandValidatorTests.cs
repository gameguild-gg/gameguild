using FluentValidation.TestHelper;
using GameGuild.Identity.Authentication;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Validators;

public class GoogleIdTokenSignInCommandValidatorTests
{
    private readonly GoogleIdTokenSignInCommandValidator _validator = new();

    [Fact]
    public void Should_HaveError_When_IdTokenIsEmpty()
    {
        var command = new GoogleIdTokenSignInCommand { IdToken = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.IdToken);
    }

    [Fact]
    public void Should_NotHaveError_When_IdTokenIsValid()
    {
        var command = new GoogleIdTokenSignInCommand { IdToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.payload.signature" };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.IdToken);
    }

    [Fact]
    public void Should_HaveError_When_IdTokenIsTooLong()
    {
        var command = new GoogleIdTokenSignInCommand { IdToken = new string('x', 8193) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.IdToken);
    }

    [Fact]
    public void Should_HaveError_When_TenantIdIsEmptyGuid()
    {
        var command = new GoogleIdTokenSignInCommand { IdToken = "valid-token", TenantId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Should_NotHaveError_When_TenantIdIsNull()
    {
        var command = new GoogleIdTokenSignInCommand { IdToken = "valid-token", TenantId = null };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Should_PassValidation_When_AllFieldsValid()
    {
        var command = new GoogleIdTokenSignInCommand { IdToken = "valid-token", TenantId = Guid.NewGuid() };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
