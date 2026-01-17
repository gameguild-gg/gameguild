using FluentAssertions;
using System.Reflection;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Services;

public class ApplePayWebhookPayloadTests
{
    [Fact]
    public void ParseApplePayPayloadData_Should_Map_Payment_Payload()
    {
        var json = "{\"eventType\":\"payment_completed\",\"transactionId\":\"tx_1\",\"amount\":\"12.50\",\"currency\":\"EUR\"}";
        var method = typeof(ApplePayBillingWebhookService)
            .GetMethod("ParseApplePayPayloadData", BindingFlags.NonPublic | BindingFlags.Static);

        var payload = method!.Invoke(null, new object[] { json });
        var toPayment = payload!.GetType().GetMethod("ToPaymentPayload")!.Invoke(payload, null) as ApplePayPaymentWebhookPayload;

        toPayment.Should().NotBeNull();
        toPayment!.PaymentId.Should().Be("tx_1");
        toPayment.Currency.Should().Be("EUR");
    }

    [Fact]
    public void ParseApplePayPayloadData_Should_Map_Subscription_Payload()
    {
        var json = "{\"eventType\":\"subscription_created\",\"transactionId\":\"tx_2\"}";
        var method = typeof(ApplePayBillingWebhookService)
            .GetMethod("ParseApplePayPayloadData", BindingFlags.NonPublic | BindingFlags.Static);

        var payload = method!.Invoke(null, new object[] { json });
        var toSubscription = payload!.GetType().GetMethod("ToSubscriptionPayload")!.Invoke(payload, null) as ApplePaySubscriptionWebhookPayload;

        toSubscription.Should().NotBeNull();
        toSubscription!.ExternalSubscriptionId.Should().Be("tx_2");
    }
}