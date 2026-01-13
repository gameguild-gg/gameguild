using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Event raised when a suspended subscription is reactivated
/// </summary>
public sealed class SubscriptionReactivatedEvent(Guid subscriptionId, Guid tenantId) : DomainEvent
{
    public Guid SubscriptionId { get; } = subscriptionId;

    public Guid TenantId { get; } = tenantId;
}
