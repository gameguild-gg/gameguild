using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Models;

public class WebhookHeadersTests
{
    [Fact]
    public void PayPalWebhookHeaders_IsValid_Should_Check_Required_Fields()
    {
        var headers = new PayPalWebhookHeaders("tx", "time", "sig");

        headers.IsValid.Should().BeTrue();
    }

    [Fact]
    public void PayPalWebhookHeaders_FromHeaders_Should_Copy_Values()
    {
        var headers = PayPalWebhookHeaders.FromHeaders("tx", "time", "sig", "cert", "algo");

        headers.TransmissionId.Should().Be("tx");
        headers.AuthAlgo.Should().Be("algo");
    }

    [Fact]
    public void StripeWebhookHeaders_IsValid_Should_Require_Signature()
    {
        var headers = new StripeWebhookHeaders("sig");

        headers.IsValid.Should().BeTrue();
    }

    [Fact]
    public void AppleNotificationHeaders_IsValid_Should_Require_Payload()
    {
        var headers = new AppleNotificationHeaders("payload");

        headers.IsValid.Should().BeTrue();
    }
}