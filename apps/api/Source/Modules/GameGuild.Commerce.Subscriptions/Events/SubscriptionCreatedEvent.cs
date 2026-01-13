using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Event raised when a subscription is created
/// </summary>
public sealed class SubscriptionCreatedEvent(Guid subscriptionId, Guid tenantId, Guid planId) : DomainEvent
{
    public Guid SubscriptionId { get; } = subscriptionId;

    public Guid TenantId { get; } = tenantId;

    public Guid PlanId { get; } = planId;
}
