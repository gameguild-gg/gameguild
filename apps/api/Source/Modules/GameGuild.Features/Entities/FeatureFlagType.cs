namespace GameGuild.Features;

/// <summary>
///     Represents the different types of feature flags
/// </summary>
public enum FeatureFlagType
{
    /// <summary>
    ///     Boolean toggle feature flag
    /// </summary>
    Toggle = 0,

    /// <summary>
    ///     Feature flag with numeric value
    /// </summary>
    Numeric = 1,

    /// <summary>
    ///     Feature flag with string value
    /// </summary>
    String = 2,

    /// <summary>
    ///     Percentage rollout feature flag
    /// </summary>
    Percentage = 3,

    /// <summary>
    ///     User segment targeting feature flag
    /// </summary>
    UserSegment = 4
}
