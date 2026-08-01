namespace GameGuild.Features;

/// <summary>
///     Request for updating feature flag targeting rules
/// </summary>
public abstract class FeatureFlagTargetingRequest
{
    /// <summary>
    ///     The feature flag ID
    /// </summary>
    public Guid FeatureFlagId { get; set; }

    /// <summary>
    ///     The feature flag key
    /// </summary>
    public string FeatureKey { get; set; } = string.Empty;

    /// <summary>
    ///     Target type (e.g., "user", "tenant", "country", "plan")
    /// </summary>
    public string? TargetType { get; set; }

    /// <summary>
    ///     Target identifier (user ID, tenant ID, country code, etc.)
    /// </summary>
    public string? TargetIdentifier { get; set; }

    /// <summary>
    ///     Whether the targeting rule is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    ///     Percentage rollout (0-100)
    /// </summary>
    public int? RolloutPercentage { get; set; }

    /// <summary>
    ///     Custom value for the target
    /// </summary>
    public string? CustomValue { get; set; }

    /// <summary>
    ///     Priority of the targeting rule (higher = evaluated first)
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    ///     Additional metadata as dictionary
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    ///     User IDs to target
    /// </summary>
    public List<Guid> TargetUserIds { get; set; } = new List<Guid>();

    /// <summary>
    ///     Tenant IDs to target
    /// </summary>
    public List<Guid> TargetTenantIds { get; set; } = new List<Guid>();

    /// <summary>
    ///     Countries to target (ISO country codes)
    /// </summary>
    public List<string> TargetCountries { get; set; } = new List<string>();

    /// <summary>
    ///     Subscription plans to target
    /// </summary>
    public List<string> TargetPlans { get; set; } = new List<string>();

    /// <summary>
    ///     Custom targeting rules
    /// </summary>
    public List<TargetingRule> CustomRules { get; set; } = new List<TargetingRule>();
}
