using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.UnitTests;

public sealed class CommerceCoverageCompletionTests
{
    [Fact]
    public void SubscriptionPaymentContext_ShouldExposePaymentFields()
    {
        var subscriptionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var context = new SubscriptionPaymentContext(subscriptionId, tenantId, 42.50m, "USD", "cus_123");

        context.SubscriptionId.Should().Be(subscriptionId);
        context.TenantId.Should().Be(tenantId);
        context.Amount.Should().Be(42.50m);
        context.Currency.Should().Be("USD");
        context.ExternalCustomerId.Should().Be("cus_123");
    }
}
