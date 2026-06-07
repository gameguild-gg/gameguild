namespace GameGuild.Features;

/// <summary>
///     Subscription plan information for feature access control
/// </summary>
public abstract class SubscriptionPlan
{
    /// <summary>
    ///     The unique identifier of the subscription plan
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    ///     The name of the subscription plan
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     The tier level of the plan (e.g., Basic, Premium, Enterprise)
    /// </summary>
    public string Tier { get; set; } = string.Empty;

    /// <summary>
    ///     The maximum number of features allowed for this plan
    /// </summary>
    public int MaxFeatures { get; set; }

    /// <summary>
    ///     Whether advanced features are enabled
    /// </summary>
    public bool AdvancedFeaturesEnabled { get; set; }

    /// <summary>
    ///     Whether custom targeting is allowed
    /// </summary>
    public bool CustomTargetingEnabled { get; set; }

    /// <summary>
    ///     Whether analytics and reporting are available
    /// </summary>
    public bool AnalyticsEnabled { get; set; }

    /// <summary>
    ///     The priority level for feature evaluation (higher values = higher priority)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    ///     When the subscription expires
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    ///     Whether the subscription is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;
}
