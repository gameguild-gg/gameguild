using GameGuild.CQRS;

namespace GameGuild.Modules.Subscriptions.Events;

/// <summary>
///     Event raised when subscription is nearing usage limits
/// </summary>
public sealed class SubscriptionUsageWarningEvent : DomainEvent
{
  public SubscriptionUsageWarningEvent(Guid subscriptionId, Guid tenantId, string limitType, object currentUsage, object limit, decimal usagePercentage)
    : base(subscriptionId, "Subscription")
  {
    SubscriptionId = subscriptionId;
    TenantId = tenantId;
    LimitType = limitType;
    CurrentUsage = currentUsage;
    Limit = limit;
    UsagePercentage = usagePercentage;
  }

    public Guid SubscriptionId { get; }

    public Guid TenantId { get; }

    public string LimitType { get; }

    public object CurrentUsage { get; }

    public object Limit { get; }

    public decimal UsagePercentage { get; }
}

