using GameGuild.CQRS;
using GameGuild.Subscriptions.Models;

namespace GameGuild.Subscriptions.Events;

/// <summary>
///     Domain event raised when a subscription is cancelled
/// </summary>
public class SubscriptionCancelledEvent(Guid subscriptionId, Guid tenantId, CancellationReason reason, SubscriptionStatus previousStatus) : DomainEvent
{
    public Guid SubscriptionId { get; } = subscriptionId;

    public Guid TenantId { get; } = tenantId;

    public CancellationReason Reason { get; } = reason;

    public SubscriptionStatus PreviousStatus { get; } = previousStatus;
}
