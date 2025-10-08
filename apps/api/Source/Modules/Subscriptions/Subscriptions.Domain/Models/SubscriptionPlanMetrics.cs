using GameGuild.Shared;
namespace GameGuild.Modules.Subscriptions.Models;

/// <summary>
///     Metrics for a specific subscription plan
/// </summary>
public class SubscriptionPlanMetrics
{
    /// <summary>
    ///     Plan ID
    /// </summary>
    public Guid PlanId { get; init; }

    /// <summary>
    ///     Plan name
    /// </summary>
    public string PlanName { get; init; } = string.Empty;

    /// <summary>
    ///     Number of active subscriptions for this plan
    /// </summary>
    public int ActiveCount { get; init; }

    /// <summary>
    ///     Number of trialing subscriptions for this plan
    /// </summary>
    public int TrialingCount { get; init; }

    /// <summary>
    ///     Total revenue from this plan
    /// </summary>
    public Money Revenue { get; init; } = Money.Zero();

    /// <summary>
    ///     New subscriptions for this plan in period
    /// </summary>
    public int NewSubscriptions { get; init; }

    /// <summary>
    ///     Cancellations for this plan in period
    /// </summary>
    public int Cancellations { get; init; }
}

