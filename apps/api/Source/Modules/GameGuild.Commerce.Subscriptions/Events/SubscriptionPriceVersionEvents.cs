using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Domain event raised when a subscription is locked to a specific price version.
///     This ensures the subscription continues at the contracted rate even if the plan price changes.
/// </summary>
public class SubscriptionPriceVersionLockedEvent(Guid subscriptionId, Guid tenantId, Guid priceVersionId) : DomainEvent
{
    /// <summary>
    ///     The subscription that was locked
    /// </summary>
    public Guid SubscriptionId { get; } = subscriptionId;

    /// <summary>
    ///     The tenant owning the subscription
    /// </summary>
    public Guid TenantId { get; } = tenantId;

    /// <summary>
    ///     The price version ID the subscription is now locked to
    /// </summary>
    public Guid PriceVersionId { get; } = priceVersionId;
}

/// <summary>
///     Domain event raised when a subscription's price version lock is removed.
///     The subscription will now use the current plan price on renewal.
/// </summary>
public class SubscriptionPriceVersionUnlockedEvent(Guid subscriptionId, Guid tenantId, Guid previousPriceVersionId) : DomainEvent
{
    /// <summary>
    ///     The subscription that was unlocked
    /// </summary>
    public Guid SubscriptionId { get; } = subscriptionId;

    /// <summary>
    ///     The tenant owning the subscription
    /// </summary>
    public Guid TenantId { get; } = tenantId;

    /// <summary>
    ///     The price version ID the subscription was previously locked to
    /// </summary>
    public Guid PreviousPriceVersionId { get; } = previousPriceVersionId;
}
