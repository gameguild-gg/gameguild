using GameGuild.CQRS;

namespace GameGuild.Modules.Subscriptions.Events;

/// <summary>
///     Event raised when a subscription upgrade is processed
/// </summary>
public sealed class SubscriptionUpgradedEvent : DomainEvent {
    public SubscriptionUpgradedEvent(Guid subscriptionId, Guid tenantId, Guid oldPlanId, Guid newPlanId, Money priceDifference)
      : base(subscriptionId, "Subscription") {
        SubscriptionId = subscriptionId;
        TenantId = tenantId;
        OldPlanId = oldPlanId;
        NewPlanId = newPlanId;
        PriceDifference = priceDifference;
    }

    public Guid SubscriptionId { get; }

    public Guid TenantId { get; }

    public Guid OldPlanId { get; }

    public Guid NewPlanId { get; }

    public Money PriceDifference { get; }
}

