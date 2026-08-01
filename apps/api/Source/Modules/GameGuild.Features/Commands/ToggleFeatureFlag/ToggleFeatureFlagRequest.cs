namespace GameGuild.Features;

/// <summary>
///     Request to toggle a feature flag (enable/disable)
/// </summary>
public sealed class ToggleFeatureFlagRequest
{
    /// <summary>
    ///     Feature flag ID to toggle
    /// </summary>
    public Guid FeatureFlagId { get; set; }

    /// <summary>
    ///     Optional reason for toggling
    /// </summary>
    public string? Reason { get; set; }
}
