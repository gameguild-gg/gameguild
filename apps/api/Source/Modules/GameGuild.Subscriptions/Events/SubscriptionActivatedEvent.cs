using GameGuild.CQRS;

namespace GameGuild.Subscriptions.Events;

/// <summary>
///     Domain event raised when a subscription is activated
/// </summary>
public class SubscriptionActivatedEvent(Guid subscriptionId, Guid tenantId) : DomainEvent
{
    public Guid SubscriptionId { get; } = subscriptionId;

    public Guid TenantId { get; } = tenantId;
}
