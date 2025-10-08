using MediatR;
using GameGuild.Shared;
using GameGuild.Modules.Subscriptions.Models;

namespace GameGuild.Modules.Subscriptions.Events;

/// <summary>
///     Domain event raised when a subscription is cancelled
/// </summary>
public class SubscriptionCancelledEvent : DomainEvent
{
    public SubscriptionCancelledEvent(Guid subscriptionId, Guid tenantId, CancellationReason reason, SubscriptionStatus previousStatus)
    {
        SubscriptionId = subscriptionId;
        TenantId = tenantId;
        Reason = reason;
        PreviousStatus = previousStatus;
    }

    public Guid SubscriptionId { get; }

    public Guid TenantId { get; }

    public CancellationReason Reason { get; }

    public SubscriptionStatus PreviousStatus { get; }
}

