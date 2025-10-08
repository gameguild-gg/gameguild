using MediatR;

namespace GameGuild.Modules.Subscriptions.Events;

/// <summary>
///     Event raised when a trial period starts
/// </summary>
public sealed class TrialStartedEvent : DomainEvent
{
    public TrialStartedEvent(Guid subscriptionId, Guid tenantId, DateTime trialEndDate)
    {
        SubscriptionId = subscriptionId;
        TenantId = tenantId;
        TrialEndDate = trialEndDate;
    }

    public Guid SubscriptionId { get; }

    public Guid TenantId { get; }

    public DateTime TrialEndDate { get; }
}

