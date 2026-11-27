using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests;

public sealed class MfaSetupRequestValidationTests
{
    [Fact]
    public void SmsConfirmationAcceptsTheDeliveredCodeWithoutAnAuthenticatorSecret()
    {
        var request = JsonSerializer.Deserialize<CompleteSmsMfaSetupRequest>(
            "{\"code\":\"123456\"}", new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(request);
        Assert.True(Validator.TryValidateObject(request, new ValidationContext(request), [], true));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void SmsConfirmationStillRequiresANonblankCode(string code)
    {
        var request = new CompleteSmsMfaSetupRequest { Code = code };
        var errors = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), errors, true));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(CompleteSmsMfaSetupRequest.Code)));
    }

    [Fact]
    public void SmsConfirmationRejectsAnOversizedCode()
    {
        var request = new CompleteSmsMfaSetupRequest { Code = new string('1', 65) };
        var errors = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), errors, true));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(CompleteSmsMfaSetupRequest.Code)));
    }

    [Fact]
    public void AuthenticatorConfirmationStillRequiresItsSecretKey()
    {
        var request = new CompleteMfaSetupRequest { Code = "123456" };
        var errors = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), errors, true));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(CompleteMfaSetupRequest.SecretKey)));
    }

    // The email factor has no authenticator secret, so its completion contract carries
    // the mailed code alone (issue #1298 J2) — like SMS, unlike the authenticator app.
    [Fact]
    public void EmailConfirmationAcceptsTheMailedCodeWithoutAnAuthenticatorSecret()
    {
        var request = JsonSerializer.Deserialize<CompleteEmailMfaSetupRequest>(
            "{\"code\":\"123456\"}", new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(request);
        Assert.True(Validator.TryValidateObject(request, new ValidationContext(request), [], true));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void EmailConfirmationStillRequiresANonblankCode(string code)
    {
        var request = new CompleteEmailMfaSetupRequest { Code = code };
        var errors = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), errors, true));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(CompleteEmailMfaSetupRequest.Code)));
    }

    [Fact]
    public void EmailConfirmationRejectsAnOversizedCode()
    {
        var request = new CompleteEmailMfaSetupRequest { Code = new string('1', 65) };
        var errors = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), errors, true));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(CompleteEmailMfaSetupRequest.Code)));
    }
}
