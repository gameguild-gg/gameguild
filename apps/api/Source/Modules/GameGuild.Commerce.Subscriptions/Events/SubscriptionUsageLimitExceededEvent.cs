using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Event raised when subscription usage exceeds limits
/// </summary>
public sealed class SubscriptionUsageLimitExceededEvent(Guid subscriptionId, Guid tenantId, string limitType, object currentUsage, object limit) : DomainEvent
{
    public Guid SubscriptionId { get; } = subscriptionId;

    public Guid TenantId { get; } = tenantId;

    public string LimitType { get; } = limitType;

    public object CurrentUsage { get; } = currentUsage;

    public object Limit { get; } = limit;
}
