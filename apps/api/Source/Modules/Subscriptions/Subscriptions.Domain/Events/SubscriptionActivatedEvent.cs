using MediatR;

namespace GameGuild.Modules.Subscriptions.Events;

/// <summary>
///     Domain event raised when a subscription is activated
/// </summary>
public class SubscriptionActivatedEvent : DomainEvent
{
    public SubscriptionActivatedEvent(Guid subscriptionId, Guid tenantId)
    {
        SubscriptionId = subscriptionId;
        TenantId = tenantId;
    }

    public Guid SubscriptionId { get; }

    public Guid TenantId { get; }
}

