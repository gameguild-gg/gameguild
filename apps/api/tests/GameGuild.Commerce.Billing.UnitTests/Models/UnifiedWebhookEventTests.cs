using FluentAssertions;
using GameGuild.Commerce;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Models;

public class UnifiedWebhookEventTests
{
    [Fact]
    public void FromStripePayment_Should_Map_Status_And_Data()
    {
        var payload = new StripePaymentWebhookPayload
        {
            TenantId = Guid.NewGuid(),
            PaymentId = "pay_1",
            ExternalSubscriptionId = "sub_1",
            Amount = 10,
            Currency = "USD",
            Status = "succeeded",
            PaidAt = DateTime.UtcNow,
            CustomerId = "cust",
            InvoiceId = "inv"
        };

        var unified = UnifiedWebhookEvent.FromStripePayment(payload, "invoice.payment_succeeded", "evt");

        unified.Status.Should().Be(WebhookEventStatus.Success);
        unified.Provider.Should().Be(PaymentProviders.Stripe);
        unified.ProviderData.Should().ContainKey("customerId");
    }

    [Fact]
    public void FromPayPalPayment_Should_Map_Status_Failed()
    {
        var payload = new PayPalPaymentWebhookPayload
        {
            TenantId = Guid.NewGuid(),
            PaymentId = "pay_1",
            ExternalSubscriptionId = "sub_1",
            Amount = 10,
            Currency = "USD",
            Status = "failed",
            PaidAt = DateTime.UtcNow
        };

        var unified = UnifiedWebhookEvent.FromPayPalPayment(payload, "PAYMENT.SALE.DENIED", "evt");

        unified.Status.Should().Be(WebhookEventStatus.Failed);
        unified.Provider.Should().Be(PaymentProviders.PayPal);
    }

    [Fact]
    public void FromStripeSubscription_Should_Map_Status_Unknown()
    {
        var payload = new StripeSubscriptionWebhookPayload
        {
            TenantId = Guid.NewGuid(),
            ExternalSubscriptionId = "sub_1",
            Status = "mystery",
            Amount = 10
        };

        var unified = UnifiedWebhookEvent.FromStripeSubscription(payload, "customer.subscription.updated", "evt");

        unified.Status.Should().Be(WebhookEventStatus.Unknown);
        unified.Provider.Should().Be(PaymentProviders.Stripe);
    }

    [Theory]
    [InlineData("pending", WebhookEventStatus.Pending)]
    [InlineData("canceled", WebhookEventStatus.Canceled)]
    [InlineData("refunded", WebhookEventStatus.Refunded)]
    public void FromStripeSubscription_Should_Map_Additional_Statuses(string status, WebhookEventStatus expected)
    {
        var payload = new StripeSubscriptionWebhookPayload
        {
            TenantId = Guid.NewGuid(),
            ExternalSubscriptionId = "sub_1",
            Status = status,
            Amount = 10
        };

        var unified = UnifiedWebhookEvent.FromStripeSubscription(payload, "customer.subscription.updated", "evt");

        unified.Status.Should().Be(expected);
    }
}