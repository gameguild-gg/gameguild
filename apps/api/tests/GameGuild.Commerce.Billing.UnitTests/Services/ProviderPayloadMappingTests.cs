using FluentAssertions;
using System.Reflection;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Services;

public class ProviderPayloadMappingTests
{
    [Fact]
    public void StripeWebhookPayload_Should_Default_Nulls_On_Payment()
    {
        var type = typeof(StripeBillingWebhookService).Assembly.GetType("GameGuild.Commerce.Billing.StripeWebhookPayload");
        var payload = Activator.CreateInstance(type!);

        type!.GetProperty("TenantId")!.SetValue(payload, null);
        type.GetProperty("Amount")!.SetValue(payload, null);
        type.GetProperty("Currency")!.SetValue(payload, null);
        type.GetProperty("ExternalSubscriptionId")!.SetValue(payload, null);
        type.GetProperty("PaymentId")!.SetValue(payload, null);

        var method = type.GetMethod("ToPaymentPayload", BindingFlags.Instance | BindingFlags.Public)!;
        var result = (StripePaymentWebhookPayload)method.Invoke(payload, null)!;

        result.Currency.Should().Be("USD");
        result.ExternalSubscriptionId.Should().BeEmpty();
    }

    [Fact]
    public void PayPalWebhookPayload_Should_Default_Nulls_On_Payment()
    {
        var type = typeof(PayPalBillingWebhookService).Assembly.GetType("GameGuild.Commerce.Billing.PayPalWebhookPayload");
        var payload = Activator.CreateInstance(type!);

        type!.GetProperty("ResourceId")!.SetValue(payload, null);
        type.GetProperty("PaymentId")!.SetValue(payload, null);
        type.GetProperty("SubscriptionId")!.SetValue(payload, null);
        type.GetProperty("Currency")!.SetValue(payload, null);

        var method = type.GetMethod("ToPaymentPayload", BindingFlags.Instance | BindingFlags.Public)!;
        var result = (PayPalPaymentWebhookPayload)method.Invoke(payload, null)!;

        result.Currency.Should().Be("USD");
        result.PaymentId.Should().BeEmpty();
    }

    [Fact]
    public void ApplePayWebhookPayload_Should_Default_Nulls_On_Subscription()
    {
        var type = typeof(ApplePayBillingWebhookService).Assembly.GetType("GameGuild.Commerce.Billing.ApplePayWebhookPayload");
        var payload = Activator.CreateInstance(type!);

        type!.GetProperty("SubscriptionId")!.SetValue(payload, null);
        type.GetProperty("TransactionId")!.SetValue(payload, null);

        var method = type.GetMethod("ToSubscriptionPayload", BindingFlags.Instance | BindingFlags.Public)!;
        var result = (ApplePaySubscriptionWebhookPayload)method.Invoke(payload, null)!;

        result.ExternalSubscriptionId.Should().BeEmpty();
    }
}