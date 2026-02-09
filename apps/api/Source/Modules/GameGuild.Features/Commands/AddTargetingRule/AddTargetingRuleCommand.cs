using GameGuild.CQRS;

namespace GameGuild.Features;

/// <summary>
///     Command to add a targeting rule to a feature flag
/// </summary>
public sealed record AddTargetingRuleCommand : ICommand<Guid>
{
    /// <summary>
    ///     Feature flag ID to add targeting rule to
    /// </summary>
    public required Guid FeatureFlagId { get; init; }

    /// <summary>
    ///     Type of target (Tenant, User, Plan, Country, Custom)
    /// </summary>
    public required string TargetType { get; init; }

    /// <summary>
    ///     Target identifier (ID or value)
    /// </summary>
    public required string TargetIdentifier { get; init; }

    /// <summary>
    ///     Whether the feature is enabled for this target
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    ///     Rollout percentage (0-100)
    /// </summary>
    public int RolloutPercentage { get; init; } = 100;

    /// <summary>
    ///     Custom value override
    /// </summary>
    public string? CustomValue { get; init; }

    /// <summary>
    ///     Rule priority (higher executes first)
    /// </summary>
    public int Priority { get; init; }

    /// <summary>
    ///     Additional metadata for the rule
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}
