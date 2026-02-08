
namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Revenue analytics data
/// </summary>
public abstract class RevenueAnalytics
{
    /// <summary>
    ///     Total revenue for the period
    /// </summary>
    public Money TotalRevenue { get; init; } = Money.Zero();

    /// <summary>
    ///     Revenue from new subscriptions
    /// </summary>
    public Money NewSubscriptionRevenue { get; init; } = Money.Zero();

    /// <summary>
    ///     Revenue from renewals
    /// </summary>
    public Money RenewalRevenue { get; init; } = Money.Zero();

    /// <summary>
    ///     Revenue from upgrades
    /// </summary>
    public Money UpgradeRevenue { get; init; } = Money.Zero();

    /// <summary>
    ///     Revenue from add-ons
    /// </summary>
    public Money AddOnRevenue { get; init; } = Money.Zero();

    /// <summary>
    ///     Amount refunded
    /// </summary>
    public Money RefundAmount { get; init; } = Money.Zero();

    /// <summary>
    ///     Net revenue (total - refunds)
    /// </summary>
    public Money NetRevenue { get => TotalRevenue - RefundAmount; }

    /// <summary>
    ///     Analysis period start
    /// </summary>
    public DateTime PeriodStart { get; init; }

    /// <summary>
    ///     Analysis period end
    /// </summary>
    public DateTime PeriodEnd { get; init; }

    /// <summary>
    ///     Revenue breakdown by billing cycle
    /// </summary>
    public Dictionary<BillingCycle, Money> BillingCycleBreakdown { get; init; } = new Dictionary<BillingCycle, Money>();

    /// <summary>
    ///     Revenue breakdown by subscription plan
    /// </summary>
    public Dictionary<Guid, Money> PlanBreakdown { get; init; } = new Dictionary<Guid, Money>();

    /// <summary>
    ///     Number of transactions processed
    /// </summary>
    public int TransactionCount { get; init; }

    /// <summary>
    ///     Average transaction value
    /// </summary>
    public Money AverageTransactionValue { get => TransactionCount > 0 ? TotalRevenue / TransactionCount : Money.Zero(); }
}
