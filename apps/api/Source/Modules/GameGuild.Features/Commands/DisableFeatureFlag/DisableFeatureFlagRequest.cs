namespace GameGuild.Features;

/// <summary>
///     Request to disable a feature flag
/// </summary>
public sealed class DisableFeatureFlagRequest
{
    /// <summary>
    ///     Feature flag ID to disable
    /// </summary>
    public Guid FeatureFlagId { get; set; }

    /// <summary>
    ///     Optional reason for disabling
    /// </summary>
    public string? Reason { get; set; }
}
