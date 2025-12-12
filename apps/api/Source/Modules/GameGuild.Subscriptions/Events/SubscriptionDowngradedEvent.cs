using GameGuild.CQRS;

namespace GameGuild.Subscriptions.Events;

/// <summary>
///     Event raised when a subscription downgrade is processed
/// </summary>
public sealed class SubscriptionDowngradedEvent(Guid subscriptionId, Guid tenantId, Guid oldPlanId, Guid newPlanId, Money priceDifference) : DomainEvent
{
    public Guid SubscriptionId { get; } = subscriptionId;

    public Guid TenantId { get; } = tenantId;

    public Guid OldPlanId { get; } = oldPlanId;

    public Guid NewPlanId { get; } = newPlanId;

    public Money PriceDifference { get; } = priceDifference;
}
