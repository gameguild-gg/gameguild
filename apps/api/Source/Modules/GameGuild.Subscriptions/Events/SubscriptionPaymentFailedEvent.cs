using GameGuild.CQRS;

namespace GameGuild.Subscriptions.Events;

/// <summary>
///     Event raised when a subscription payment fails
/// </summary>
public sealed class SubscriptionPaymentFailedEvent(Guid subscriptionId, Guid tenantId, string reason, DateTime failureDate) : DomainEvent
{
    public Guid SubscriptionId { get; } = subscriptionId;

    public Guid TenantId { get; } = tenantId;

    public string Reason { get; } = reason;

    public DateTime FailureDate { get; } = failureDate;
}
