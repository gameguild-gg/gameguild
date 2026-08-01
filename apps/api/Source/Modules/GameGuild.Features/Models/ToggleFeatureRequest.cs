namespace GameGuild.Features;

/// <summary>
///     Request for toggling a feature flag on/off
/// </summary>
public class ToggleFeatureRequest
{
    /// <summary>
    ///     The feature flag key
    /// </summary>
    public string FeatureKey { get; set; } = string.Empty;

    /// <summary>
    ///     Whether to enable or disable the feature
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    ///     Optional reason for the toggle
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    ///     Optional tenant ID (for tenant-specific toggles)
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    ///     Optional environment (for environment-specific toggles)
    /// </summary>
    public string? Environment { get; set; }
}
