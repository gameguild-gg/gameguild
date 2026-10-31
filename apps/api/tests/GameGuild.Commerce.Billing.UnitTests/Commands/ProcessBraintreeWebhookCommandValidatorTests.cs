using FluentValidation.TestHelper;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Commands;

public class ProcessBraintreeWebhookCommandValidatorTests
{
    private readonly ProcessBraintreeWebhookCommandValidator _validator = new();

    [Fact]
    public void Should_Pass_For_WellFormed_Command()
    {
        var command = new ProcessBraintreeWebhookCommand("key|abcdef0123456789", "cGF5bG9hZA==");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Should_Fail_When_Signature_Missing(string? signature)
    {
        var command = new ProcessBraintreeWebhookCommand(signature!, "cGF5bG9hZA==");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.BtSignature);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Should_Fail_When_Payload_Missing(string? payload)
    {
        var command = new ProcessBraintreeWebhookCommand("key|abcdef0123456789", payload!);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.BtPayload);
    }

    [Fact]
    public void Should_Fail_When_Signature_Exceeds_Bound()
    {
        var command = new ProcessBraintreeWebhookCommand(new string('a', 513), "cGF5bG9hZA==");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.BtSignature);
    }

    [Fact]
    public void Should_Fail_When_Payload_Exceeds_Bound()
    {
        var command = new ProcessBraintreeWebhookCommand("key|abcdef0123456789", new string('a', 1_000_001));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.BtPayload);
    }
}
