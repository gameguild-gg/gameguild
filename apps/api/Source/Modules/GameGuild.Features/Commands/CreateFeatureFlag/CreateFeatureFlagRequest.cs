namespace GameGuild.Features;

/// <summary>
///     Request to create a new feature flag
/// </summary>
public class CreateFeatureFlagRequest
{
    /// <summary>
    ///     Unique key for the feature flag
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    ///     Display name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     Feature type
    /// </summary>
    public FeatureFlagType Type { get; set; }

    /// <summary>
    ///     Default value
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    ///     Is enabled by default
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    ///     Rollout percentage
    /// </summary>
    public int RolloutPercentage { get; set; } = 100;

    /// <summary>
    ///     Environment
    /// </summary>
    public string Environment { get; set; } = "production";

    /// <summary>
    ///     Tags for categorization
    /// </summary>
    public IEnumerable<string>? Tags { get; set; }
}
