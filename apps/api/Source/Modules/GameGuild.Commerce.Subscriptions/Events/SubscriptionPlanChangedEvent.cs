using GameGuild.CQRS;
using GameGuild.ValueObjects;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Event raised when a subscription plan is changed
/// </summary>
public sealed class SubscriptionPlanChangedEvent(Guid subscriptionId, Guid tenantId, Guid oldPlanId, Guid newPlanId, Money oldAmount, Money newAmount) : DomainEvent
{
    public Guid SubscriptionId { get; } = subscriptionId;

    public Guid TenantId { get; } = tenantId;

    public Guid OldPlanId { get; } = oldPlanId;

    public Guid NewPlanId { get; } = newPlanId;

    public Money OldAmount { get; } = oldAmount;

    public Money NewAmount { get; } = newAmount;
}
