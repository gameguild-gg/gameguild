using GameGuild.CQRS;


namespace GameGuild.Modules.Subscriptions.Events;

/// <summary>
///     Event raised when a subscription billing cycle is changed
/// </summary>
public sealed class SubscriptionBillingCycleChangedEvent : DomainEvent {
    public SubscriptionBillingCycleChangedEvent(Guid subscriptionId, Guid tenantId, BillingCycle oldBillingCycle, BillingCycle newBillingCycle, Money oldAmount, Money newAmount)
      : base(subscriptionId, "Subscription") {
        SubscriptionId = subscriptionId;
        TenantId = tenantId;
        OldBillingCycle = oldBillingCycle;
        NewBillingCycle = newBillingCycle;
        OldAmount = oldAmount;
        NewAmount = newAmount;
    }

    public Guid SubscriptionId { get; }

    public Guid TenantId { get; }

    public BillingCycle OldBillingCycle { get; }

    public BillingCycle NewBillingCycle { get; }

    public Money OldAmount { get; }

    public Money NewAmount { get; }
}

