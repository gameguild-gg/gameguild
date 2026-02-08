
namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Subscription analytics data
/// </summary>
public abstract class SubscriptionAnalytics
{
    /// <summary>
    ///     Total number of subscriptions
    /// </summary>
    public int TotalSubscriptions { get; init; }

    /// <summary>
    ///     Number of active subscriptions
    /// </summary>
    public int ActiveSubscriptions { get; init; }

    /// <summary>
    ///     Number of trialing subscriptions
    /// </summary>
    public int TrialingSubscriptions { get; init; }

    /// <summary>
    ///     Number of cancelled subscriptions
    /// </summary>
    public int CancelledSubscriptions { get; init; }

    /// <summary>
    ///     Number of suspended subscriptions
    /// </summary>
    public int SuspendedSubscriptions { get; init; }

    /// <summary>
    ///     New subscriptions created in period
    /// </summary>
    public int NewSubscriptions { get; init; }

    /// <summary>
    ///     Subscriptions cancelled in period
    /// </summary>
    public int CancellationsInPeriod { get; init; }

    /// <summary>
    ///     Churn rate (cancellations / total active at start of period)
    /// </summary>
    public decimal ChurnRate { get; init; }

    /// <summary>
    ///     Growth rate (new subscriptions - cancellations) / total at start
    /// </summary>
    public decimal GrowthRate { get; init; }

    /// <summary>
    ///     Analysis period start
    /// </summary>
    public DateTime PeriodStart { get; init; }

    /// <summary>
    ///     Analysis period end
    /// </summary>
    public DateTime PeriodEnd { get; init; }

    /// <summary>
    ///     Breakdown by subscription plan
    /// </summary>
    public Dictionary<Guid, SubscriptionPlanMetrics> PlanBreakdown { get; init; } = new Dictionary<Guid, SubscriptionPlanMetrics>();

    /// <summary>
    ///     Breakdown by billing cycle
    /// </summary>
    public Dictionary<BillingCycle, int> BillingCycleBreakdown { get; init; } = new Dictionary<BillingCycle, int>();

    /// <summary>
    ///     Monthly recurring revenue
    /// </summary>
    public Money MonthlyRecurringRevenue { get; init; } = Money.Zero();

    /// <summary>
    ///     Annual recurring revenue
    /// </summary>
    public Money AnnualRecurringRevenue { get; init; } = Money.Zero();

    /// <summary>
    ///     Average revenue per user
    /// </summary>
    public Money AverageRevenuePerUser { get => ActiveSubscriptions > 0 ? MonthlyRecurringRevenue / ActiveSubscriptions : Money.Zero(); }
}
