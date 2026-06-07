using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Domain event raised when a subscription is renewed
/// </summary>
public class SubscriptionRenewedEvent(Guid subscriptionId, Guid tenantId, int billingCycleCount, Money amount) : DomainEvent
{
    public Guid SubscriptionId { get; } = subscriptionId;

    public Guid TenantId { get; } = tenantId;

    public int BillingCycleCount { get; } = billingCycleCount;

    public Money Amount { get; } = amount;
}
