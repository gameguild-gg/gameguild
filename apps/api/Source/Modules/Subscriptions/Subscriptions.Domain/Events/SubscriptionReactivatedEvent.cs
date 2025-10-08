using MediatR;

namespace GameGuild.Modules.Subscriptions.Events;

/// <summary>
///     Event raised when a suspended subscription is reactivated
/// </summary>
public sealed class SubscriptionReactivatedEvent : DomainEvent
{
    public SubscriptionReactivatedEvent(Guid subscriptionId, Guid tenantId)
    {
        SubscriptionId = subscriptionId;
        TenantId = tenantId;
    }

    public Guid SubscriptionId { get; }

    public Guid TenantId { get; }
}

