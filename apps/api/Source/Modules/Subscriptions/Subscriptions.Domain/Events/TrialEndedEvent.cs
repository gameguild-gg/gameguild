using GameGuild.CQRS;

namespace GameGuild.Modules.Subscriptions.Events;

/// <summary>
///     Event raised when a trial period ends
/// </summary>
public sealed class TrialEndedEvent : DomainEvent {
    public TrialEndedEvent(Guid subscriptionId, Guid tenantId, bool convertedToPaid)
      : base(subscriptionId, "Subscription") {
        SubscriptionId = subscriptionId;
        TenantId = tenantId;
        ConvertedToPaid = convertedToPaid;
    }

    public Guid SubscriptionId { get; }

    public Guid TenantId { get; }

    public bool ConvertedToPaid { get; }
}

