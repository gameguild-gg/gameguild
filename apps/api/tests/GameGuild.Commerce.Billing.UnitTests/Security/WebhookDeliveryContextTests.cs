using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Security;

/// <summary>
///     Unit tests for the per-request webhook delivery source capture (issue #408: IP
///     address and user agent logging).
/// </summary>
public class WebhookDeliveryContextTests
{
    [Fact]
    public void Capture_Should_Store_The_First_Capture()
    {
        var context = new WebhookDeliveryContext();

        context.Capture("stripe", "203.0.113.9", "Stripe/1.0");
        context.Capture("paypal", "198.51.100.7", "PayPal/2.0");

        context.Provider.Should().Be("stripe");
        context.SourceIpAddress.Should().Be("203.0.113.9");
        context.UserAgent.Should().Be("Stripe/1.0");
    }

    [Fact]
    public void Capture_Should_Bound_The_User_Agent()
    {
        var context = new WebhookDeliveryContext();

        context.Capture(null, null, new string('u', 600));

        context.UserAgent.Should().HaveLength(512);
        context.Provider.Should().BeNull();
        context.SourceIpAddress.Should().BeNull();
    }

    [Fact]
    public void Capture_Should_Ignore_Blank_Values()
    {
        var context = new WebhookDeliveryContext();

        context.Capture("  ", "", null);

        context.Provider.Should().BeNull();
        context.SourceIpAddress.Should().BeNull();
        context.UserAgent.Should().BeNull();
    }

    [Fact]
    public void RecordDeliverySource_Should_Truncate_Values_To_Their_Column_Lengths()
    {
        var webhookEvent = new GameGuild.Commerce.Billing.BillingWebhookEvent
        {
            ExternalEventId = "evt",
            Provider = "stripe",
            EventType = "type",
            Payload = "{}"
        };

        webhookEvent.RecordDeliverySource(new string('i', 80), new string('u', 600));

        webhookEvent.SourceIpAddress.Should().HaveLength(64);
        webhookEvent.UserAgent.Should().HaveLength(512);
    }

    [Fact]
    public void RecordDeliverySource_Should_Preserve_Already_Captured_Values()
    {
        var webhookEvent = new GameGuild.Commerce.Billing.BillingWebhookEvent
        {
            ExternalEventId = "evt",
            Provider = "stripe",
            EventType = "type",
            Payload = "{}",
            SourceIpAddress = "203.0.113.9",
            UserAgent = "Stripe/1.0"
        };

        webhookEvent.RecordDeliverySource(null, null);

        webhookEvent.SourceIpAddress.Should().Be("203.0.113.9");
        webhookEvent.UserAgent.Should().Be("Stripe/1.0");
    }
}
