using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Event raised when subscription is nearing usage limits
/// </summary>
public sealed class SubscriptionUsageWarningEvent(Guid subscriptionId, Guid tenantId, string limitType, object currentUsage, object limit, decimal usagePercentage) : DomainEvent
{
    public Guid SubscriptionId { get; } = subscriptionId;

    public Guid TenantId { get; } = tenantId;

    public string LimitType { get; } = limitType;

    public object CurrentUsage { get; } = currentUsage;

    public object Limit { get; } = limit;

    public decimal UsagePercentage { get; } = usagePercentage;
}
