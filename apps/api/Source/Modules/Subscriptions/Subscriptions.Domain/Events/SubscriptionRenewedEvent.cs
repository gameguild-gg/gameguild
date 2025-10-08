using MediatR;
using GameGuild.Shared;

namespace GameGuild.Modules.Subscriptions.Events;

/// <summary>
///     Domain event raised when a subscription is renewed
/// </summary>
public class SubscriptionRenewedEvent : DomainEvent
{
    public SubscriptionRenewedEvent(Guid subscriptionId, Guid tenantId, int billingCycleCount, Money amount)
    {
        SubscriptionId = subscriptionId;
        TenantId = tenantId;
        BillingCycleCount = billingCycleCount;
        Amount = amount;
    }

    public Guid SubscriptionId { get; }

    public Guid TenantId { get; }

    public int BillingCycleCount { get; }

    public Money Amount { get; }
}

