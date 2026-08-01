namespace GameGuild.Features;

/// <summary>
///     Request DTO for adding a targeting rule to a feature flag
/// </summary>
public sealed class AddTargetingRuleRequest
{
    /// <summary>
    ///     Feature flag ID to add targeting rule to
    /// </summary>
    public required Guid FeatureFlagId { get; set; }

    /// <summary>
    ///     Type of target (Tenant, User, Plan, Country, Custom)
    /// </summary>
    public required string TargetType { get; set; }

    /// <summary>
    ///     Target identifier (ID or value)
    /// </summary>
    public required string TargetIdentifier { get; set; }

    /// <summary>
    ///     Whether the feature is enabled for this target
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    ///     Rollout percentage (0-100)
    /// </summary>
    public int RolloutPercentage { get; set; } = 100;

    /// <summary>
    ///     Custom value override
    /// </summary>
    public string? CustomValue { get; set; }

    /// <summary>
    ///     Rule priority (higher executes first)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    ///     Additional metadata for the rule
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}
