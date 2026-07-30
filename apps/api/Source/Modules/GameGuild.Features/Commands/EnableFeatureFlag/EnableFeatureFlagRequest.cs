namespace GameGuild.Features;

/// <summary>
///     Request to enable a feature flag
/// </summary>
public sealed class EnableFeatureFlagRequest
{
    /// <summary>
    ///     Feature flag ID to enable
    /// </summary>
    public Guid FeatureFlagId { get; set; }

    /// <summary>
    ///     Optional reason for enabling
    /// </summary>
    public string? Reason { get; set; }
}
