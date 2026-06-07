using System.Collections.ObjectModel;

namespace GameGuild.Features;

/// <summary>
///     Feature flag configuration for SDK
/// </summary>
public class FeatureFlagConfig
{
    public string Key { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsEnabled { get; set; }

    public FeatureFlagType Type { get; set; }

    public string? DefaultValue { get; set; }

    public string? EnabledValue { get; set; }

    public bool IsGlobal { get; set; }

    public int RolloutPercentage { get; set; }

    public string Environment { get; set; } = string.Empty;

    public Collection<TargetingRule> TargetingRules { get; init; } = [];

    public DateTime LastModified { get; set; }
}
