using GameGuild.CQRS;

namespace GameGuild.Subscriptions.Events;

/// <summary>
///     Event raised when a trial period starts
/// </summary>
public sealed class TrialStartedEvent(Guid subscriptionId, Guid tenantId, DateTime trialEndDate) : DomainEvent
{
    public Guid SubscriptionId { get; } = subscriptionId;

    public Guid TenantId { get; } = tenantId;

    public DateTime TrialEndDate { get; } = trialEndDate;
}
