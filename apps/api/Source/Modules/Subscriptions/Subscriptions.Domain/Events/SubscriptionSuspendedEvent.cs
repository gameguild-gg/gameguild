using MediatR;

namespace GameGuild.Modules.Subscriptions.Events;

/// <summary>
///     Event raised when a subscription is suspended
/// </summary>
public sealed class SubscriptionSuspendedEvent : DomainEvent
{
    public SubscriptionSuspendedEvent(Guid subscriptionId, Guid tenantId, string? reason = null)
    {
        SubscriptionId = subscriptionId;
        TenantId = tenantId;
        Reason = reason;
    }

    public Guid SubscriptionId { get; }

    public Guid TenantId { get; }

    public string? Reason { get; }
}

