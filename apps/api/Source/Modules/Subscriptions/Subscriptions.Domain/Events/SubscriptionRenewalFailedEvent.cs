using MediatR;

namespace GameGuild.Modules.Subscriptions.Events;

/// <summary>
///     Event raised when a subscription renewal fails
/// </summary>
public sealed class SubscriptionRenewalFailedEvent : DomainEvent
{
    public SubscriptionRenewalFailedEvent(Guid subscriptionId, Guid tenantId, string reason, DateTime failedAt)
    {
        SubscriptionId = subscriptionId;
        TenantId = tenantId;
        Reason = reason;
        FailedAt = failedAt;
    }

    public Guid SubscriptionId { get; }

    public Guid TenantId { get; }

    public string Reason { get; }

    public DateTime FailedAt { get; }
}

