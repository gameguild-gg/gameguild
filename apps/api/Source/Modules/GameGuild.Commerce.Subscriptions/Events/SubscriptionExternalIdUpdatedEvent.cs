using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Event raised when a subscription's external ID is updated
/// </summary>
public sealed class SubscriptionExternalIdUpdatedEvent(Guid subscriptionId, string externalId) : DomainEvent
{
    public Guid SubscriptionId { get; } = subscriptionId;

    public string ExternalId { get; } = externalId;
}
