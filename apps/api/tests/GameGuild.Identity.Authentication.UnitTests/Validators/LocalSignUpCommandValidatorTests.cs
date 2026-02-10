using FluentAssertions;
using FluentValidation.TestHelper;
using GameGuild.Identity.Authentication;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Validators;

public class LocalSignUpCommandValidatorTests
{
    private readonly LocalSignUpCommandValidator _validator = new();

    // ── Email ─────────────────────────────────────────────────

    [Fact]
    public void Should_HaveError_When_EmailIsEmpty()
    {
        var command = new LocalSignUpCommand { Email = "", Password = "Password1!", Username = "user1" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_HaveError_When_EmailIsInvalid()
    {
        var command = new LocalSignUpCommand { Email = "not-an-email", Password = "Password1!", Username = "user1" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_HaveError_When_EmailIsTooLong()
    {
        var longEmail = new string('a', 246) + "@test.com"; // > 254 chars
        var command = new LocalSignUpCommand { Email = longEmail, Password = "Password1!", Username = "user1" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_NotHaveError_When_EmailIsValid()
    {
        var command = new LocalSignUpCommand { Email = "test@example.com", Password = "Password1!", Username = "user1" };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    // ── Password ──────────────────────────────────────────────

    [Fact]
    public void Should_HaveError_When_PasswordIsEmpty()
    {
        var command = new LocalSignUpCommand { Email = "test@example.com", Password = "", Username = "user1" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_HaveError_When_PasswordIsTooShort()
    {
        var command = new LocalSignUpCommand { Email = "test@example.com", Password = "Abc1!", Username = "user1" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_HaveError_When_PasswordIsTooLong()
    {
        var command = new LocalSignUpCommand { Email = "test@example.com", Password = new string('A', 129) + "a1!", Username = "user1" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_HaveError_When_PasswordMissingUppercase()
    {
        var command = new LocalSignUpCommand { Email = "test@example.com", Password = "password1!", Username = "user1" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_HaveError_When_PasswordMissingLowercase()
    {
        var command = new LocalSignUpCommand { Email = "test@example.com", Password = "PASSWORD1!", Username = "user1" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_HaveError_When_PasswordMissingDigit()
    {
        var command = new LocalSignUpCommand { Email = "test@example.com", Password = "Password!", Username = "user1" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_HaveError_When_PasswordMissingSpecialChar()
    {
        var command = new LocalSignUpCommand { Email = "test@example.com", Password = "Password1", Username = "user1" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_NotHaveError_When_PasswordIsValid()
    {
        var command = new LocalSignUpCommand { Email = "test@example.com", Password = "Password1!", Username = "user1" };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    // ── Username ──────────────────────────────────────────────

    [Fact]
    public void Should_HaveError_When_UsernameIsEmpty()
    {
        var command = new LocalSignUpCommand { Email = "test@example.com", Password = "Password1!", Username = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void Should_HaveError_When_UsernameIsTooShort()
    {
        var command = new LocalSignUpCommand { Email = "test@example.com", Password = "Password1!", Username = "ab" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void Should_HaveError_When_UsernameIsTooLong()
    {
        var command = new LocalSignUpCommand { Email = "test@example.com", Password = "Password1!", Username = new string('a', 51) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void Should_HaveError_When_UsernameHasInvalidChars()
    {
        var command = new LocalSignUpCommand { Email = "test@example.com", Password = "Password1!", Username = "user name!" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Theory]
    [InlineData("user_name")]
    [InlineData("user.name")]
    [InlineData("user-name")]
    [InlineData("user123")]
    public void Should_NotHaveError_When_UsernameIsValid(string username)
    {
        var command = new LocalSignUpCommand { Email = "test@example.com", Password = "Password1!", Username = username };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Username);
    }

    // ── TenantId ──────────────────────────────────────────────

    [Fact]
    public void Should_HaveError_When_TenantIdIsEmptyGuid()
    {
        var command = new LocalSignUpCommand { Email = "test@example.com", Password = "Password1!", Username = "user1", TenantId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Should_NotHaveError_When_TenantIdIsNull()
    {
        var command = new LocalSignUpCommand { Email = "test@example.com", Password = "Password1!", Username = "user1", TenantId = null };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Should_NotHaveError_When_TenantIdIsValid()
    {
        var command = new LocalSignUpCommand { Email = "test@example.com", Password = "Password1!", Username = "user1", TenantId = Guid.NewGuid() };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    // ── Full validation ───────────────────────────────────────

    [Fact]
    public void Should_PassValidation_When_AllFieldsAreValid()
    {
        var command = new LocalSignUpCommand
        {
            Email = "valid@example.com",
            Password = "StrongPass1!",
            Username = "validuser",
            TenantId = Guid.NewGuid()
        };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
