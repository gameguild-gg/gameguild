namespace GameGuild.Features;

/// <summary>
///     Request to update a feature flag
/// </summary>
public class UpdateFeatureFlagRequest
{
    /// <summary>
    ///     Updated name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     Updated description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     Updated default value
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    ///     Updated enabled state
    /// </summary>
    public bool? IsEnabled { get; set; }

    /// <summary>
    ///     Updated rollout percentage
    /// </summary>
    public int? RolloutPercentage { get; set; }

    /// <summary>
    ///     Updated tags
    /// </summary>
    public IEnumerable<string>? Tags { get; set; }
}
