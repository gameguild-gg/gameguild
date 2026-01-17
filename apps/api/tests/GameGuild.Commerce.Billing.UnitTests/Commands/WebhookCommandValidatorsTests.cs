using FluentAssertions;
using FluentValidation.Results;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Commands;

public class WebhookCommandValidatorsTests
{
    [Fact]
    public void ProcessBillingWebhookCommandValidator_Should_Reject_Invalid_Provider()
    {
        var validator = new ProcessBillingWebhookCommandValidator();
        var command = new ProcessBillingWebhookCommand("unknown", "payload", new Dictionary<string, string>());

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Provider");
    }

    [Fact]
    public void ProcessBillingWebhookCommandValidator_Should_Accept_Valid_Provider()
    {
        var validator = new ProcessBillingWebhookCommandValidator();
        var command = new ProcessBillingWebhookCommand("Stripe", "payload", new Dictionary<string, string>());

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ProcessBillingWebhookCommandValidator_Should_Reject_Too_Many_Headers()
    {
        var validator = new ProcessBillingWebhookCommandValidator();
        var headers = Enumerable.Range(0, 51).ToDictionary(i => $"h{i}", i => "v");
        var command = new ProcessBillingWebhookCommand("stripe", "payload", headers);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Headers");
    }

    [Fact]
    public void ProcessStripeWebhookCommandValidator_Should_Require_Signature()
    {
        var validator = new ProcessStripeWebhookCommandValidator();
        var command = new ProcessStripeWebhookCommand("payload", "");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Signature");
    }

    [Fact]
    public void ProcessPayPalWebhookCommandValidator_Should_Require_Transmission_Fields()
    {
        var validator = new ProcessPayPalWebhookCommandValidator();
        var command = new ProcessPayPalWebhookCommand("payload", "", "", "");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TransmissionId");
        result.Errors.Should().Contain(e => e.PropertyName == "TransmissionSignature");
        result.Errors.Should().Contain(e => e.PropertyName == "TransmissionTime");
    }

    [Fact]
    public void ProcessApplePayWebhookCommandValidator_Should_Require_Headers()
    {
        var validator = new ProcessApplePayWebhookCommandValidator();
        var command = new ProcessApplePayWebhookCommand("", "", "");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Payload");
        result.Errors.Should().Contain(e => e.PropertyName == "MerchantId");
        result.Errors.Should().Contain(e => e.PropertyName == "Signature");
    }

    [Fact]
    public void ProcessGooglePayWebhookCommandValidator_Should_Reject_Invalid_ProjectId()
    {
        var validator = new ProcessGooglePayWebhookCommandValidator();
        var command = new ProcessGooglePayWebhookCommand("payload", "auth", "BAD_");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProjectId");
    }

    [Fact]
    public void RetryWebhookEventCommandValidator_Should_Reject_Empty_EventId()
    {
        var validator = new RetryWebhookEventCommandValidator();

        var result = validator.Validate(new RetryWebhookEventCommand(""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EventId");
    }
}