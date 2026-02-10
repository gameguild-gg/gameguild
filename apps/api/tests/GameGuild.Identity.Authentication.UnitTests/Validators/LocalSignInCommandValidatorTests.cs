using FluentAssertions;
using FluentValidation.TestHelper;
using GameGuild.Identity.Authentication;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Validators;

public class LocalSignInCommandValidatorTests
{
    private readonly LocalSignInCommandValidator _validator = new();

    [Fact]
    public void Should_HaveError_When_EmailIsEmpty()
    {
        var command = new LocalSignInCommand { Email = "", Password = "pass" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_HaveError_When_EmailIsInvalid()
    {
        var command = new LocalSignInCommand { Email = "bad-email", Password = "pass" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_HaveError_When_EmailIsTooLong()
    {
        var longEmail = new string('a', 246) + "@test.com"; // > 254 chars
        var command = new LocalSignInCommand { Email = longEmail, Password = "pass" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_NotHaveError_When_EmailIsValid()
    {
        var command = new LocalSignInCommand { Email = "user@example.com", Password = "pass" };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_HaveError_When_PasswordIsEmpty()
    {
        var command = new LocalSignInCommand { Email = "user@example.com", Password = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_HaveError_When_PasswordIsTooLong()
    {
        var command = new LocalSignInCommand { Email = "user@example.com", Password = new string('x', 129) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_NotHaveError_When_PasswordIsValid()
    {
        var command = new LocalSignInCommand { Email = "user@example.com", Password = "validpass" };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_HaveError_When_TenantIdIsEmptyGuid()
    {
        var command = new LocalSignInCommand { Email = "user@example.com", Password = "pass", TenantId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Should_NotHaveError_When_TenantIdIsNull()
    {
        var command = new LocalSignInCommand { Email = "user@example.com", Password = "pass", TenantId = null };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Should_PassValidation_When_AllFieldsValid()
    {
        var command = new LocalSignInCommand { Email = "user@example.com", Password = "validpass", TenantId = Guid.NewGuid() };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
