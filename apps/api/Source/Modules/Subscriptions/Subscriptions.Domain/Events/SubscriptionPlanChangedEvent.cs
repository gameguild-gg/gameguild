using GameGuild.CQRS;

namespace GameGuild.Modules.Subscriptions.Events;

/// <summary>
///     Event raised when a subscription plan is changed
/// </summary>
public sealed class SubscriptionPlanChangedEvent : DomainEvent
{
    public SubscriptionPlanChangedEvent(Guid subscriptionId, Guid tenantId, Guid oldPlanId, Guid newPlanId, Money oldAmount, Money newAmount)
    {
        SubscriptionId = subscriptionId;
        TenantId = tenantId;
        OldPlanId = oldPlanId;
        NewPlanId = newPlanId;
        OldAmount = oldAmount;
        NewAmount = newAmount;
    }

    public Guid SubscriptionId { get; }

    public Guid TenantId { get; }

    public Guid OldPlanId { get; }

    public Guid NewPlanId { get; }

    public Money OldAmount { get; }

    public Money NewAmount { get; }
}

