using MediatR;
using GameGuild.Shared;

namespace GameGuild.Modules.Subscriptions.Events;

/// <summary>
///     Event raised when a subscription downgrade is processed
/// </summary>
public sealed class SubscriptionDowngradedEvent : DomainEvent
{
    public SubscriptionDowngradedEvent(Guid subscriptionId, Guid tenantId, Guid oldPlanId, Guid newPlanId, Money priceDifference)
    {
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

