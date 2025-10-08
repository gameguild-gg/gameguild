using MediatR;

namespace GameGuild.Modules.Subscriptions.Events;

/// <summary>
///     Event raised when a subscription is created
/// </summary>
public sealed class SubscriptionCreatedEvent : DomainEvent
{
    public SubscriptionCreatedEvent(Guid subscriptionId, Guid tenantId, Guid planId)
    {
        SubscriptionId = subscriptionId;
        TenantId = tenantId;
        PlanId = planId;
    }

    public Guid SubscriptionId { get; }

    public Guid TenantId { get; }

    public Guid PlanId { get; }
}

