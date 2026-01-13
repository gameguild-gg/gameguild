using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Event raised when a trial period ends
/// </summary>
public sealed class TrialEndedEvent(Guid subscriptionId, Guid tenantId, bool convertedToPaid) : DomainEvent
{
    public Guid SubscriptionId { get; } = subscriptionId;

    public Guid TenantId { get; } = tenantId;

    public bool ConvertedToPaid { get; } = convertedToPaid;
}
