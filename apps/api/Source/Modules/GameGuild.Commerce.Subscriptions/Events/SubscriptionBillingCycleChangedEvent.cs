using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Event raised when a subscription billing cycle is changed
/// </summary>
public sealed class SubscriptionBillingCycleChangedEvent(Guid subscriptionId, Guid tenantId, BillingCycle oldBillingCycle, BillingCycle newBillingCycle, Money oldAmount, Money newAmount) : DomainEvent
{
    public Guid SubscriptionId { get; } = subscriptionId;

    public Guid TenantId { get; } = tenantId;

    public BillingCycle OldBillingCycle { get; } = oldBillingCycle;

    public BillingCycle NewBillingCycle { get; } = newBillingCycle;

    public Money OldAmount { get; } = oldAmount;

    public Money NewAmount { get; } = newAmount;
}
