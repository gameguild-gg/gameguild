namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Usage statistics for a subscription
/// </summary>
public abstract class SubscriptionUsageStatistics
{
    /// <summary>
    ///     Subscription ID
    /// </summary>
    public Guid SubscriptionId { get; init; }

    /// <summary>
    ///     Current number of users
    /// </summary>
    public int UserCount { get; init; }

    /// <summary>
    ///     Current storage usage in MB
    /// </summary>
    public long StorageUsedMb { get; init; }

    /// <summary>
    ///     API calls made this month
    /// </summary>
    public long ApiCallsThisMonth { get; init; }

    /// <summary>
    ///     Plan limits
    /// </summary>
    public SubscriptionPlanLimits PlanLimits { get; init; } = new SubscriptionPlanLimits();

    /// <summary>
    ///     Usage period start
    /// </summary>
    public DateTime PeriodStart { get; init; }

    /// <summary>
    ///     Usage period end
    /// </summary>
    public DateTime PeriodEnd { get; init; }

    /// <summary>
    ///     When the statistics were last updated
    /// </summary>
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;
}
