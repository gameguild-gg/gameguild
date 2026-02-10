namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Usage statistics for a subscription plan
/// </summary>
public abstract class PlanUsageStatistics
{
    /// <summary>
    ///     Plan ID
    /// </summary>
    public Guid PlanId { get; set; }

    /// <summary>
    ///     Total number of active subscriptions
    /// </summary>
    public int ActiveSubscriptions { get; set; }

    /// <summary>
    ///     Total number of cancelled subscriptions
    /// </summary>
    public int CancelledSubscriptions { get; set; }

    /// <summary>
    ///     Average monthly revenue from this plan
    /// </summary>
    public decimal AverageMonthlyRevenue { get; set; }

    /// <summary>
    ///     Total revenue generated from this plan
    /// </summary>
    public decimal TotalRevenue { get; set; }

    /// <summary>
    ///     Average subscription duration in days
    /// </summary>
    public double AverageSubscriptionDurationDays { get; set; }

    /// <summary>
    ///     Conversion rate from trial to paid (if applicable)
    /// </summary>
    public decimal? TrialConversionRate { get; set; }

    /// <summary>
    ///     Churn rate for this plan
    /// </summary>
    public decimal ChurnRate { get; set; }

    /// <summary>
    ///     When these statistics were calculated
    /// </summary>
    public DateTime CalculatedAt { get; set; } = SystemClock.UtcNow;
}
