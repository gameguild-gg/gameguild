using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Event raised when a subscription is suspended
/// </summary>
public sealed class SubscriptionSuspendedEvent(Guid subscriptionId, Guid tenantId, string? reason = null) : DomainEvent
{
    public Guid SubscriptionId { get; } = subscriptionId;

    public Guid TenantId { get; } = tenantId;

    public string? Reason { get; } = reason;
}
