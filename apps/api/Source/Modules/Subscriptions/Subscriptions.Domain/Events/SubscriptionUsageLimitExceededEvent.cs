using GameGuild.CQRS;

namespace GameGuild.Modules.Subscriptions.Events;

/// <summary>
///     Event raised when subscription usage exceeds limits
/// </summary>
public sealed class SubscriptionUsageLimitExceededEvent : DomainEvent
{
    public SubscriptionUsageLimitExceededEvent(Guid subscriptionId, Guid tenantId, string limitType, object currentUsage, object limit)
    {
        SubscriptionId = subscriptionId;
        TenantId = tenantId;
        LimitType = limitType;
        CurrentUsage = currentUsage;
        Limit = limit;
    }

    public Guid SubscriptionId { get; }

    public Guid TenantId { get; }

    public string LimitType { get; }

    public object CurrentUsage { get; }

    public object Limit { get; }
}

