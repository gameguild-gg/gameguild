using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Event raised when a subscription renewal fails
/// </summary>
public sealed class SubscriptionRenewalFailedEvent(Guid subscriptionId, Guid tenantId, string reason, DateTime failedAt) : DomainEvent
{
    public Guid SubscriptionId { get; } = subscriptionId;

    public Guid TenantId { get; } = tenantId;

    public string Reason { get; } = reason;

    public DateTime FailedAt { get; } = failedAt;
}
