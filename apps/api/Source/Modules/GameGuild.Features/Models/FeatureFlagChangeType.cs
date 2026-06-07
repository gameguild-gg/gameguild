namespace GameGuild.Features;

/// <summary>
///     Feature flag change types
/// </summary>
public enum FeatureFlagChangeType
{
    Created,

    Updated,

    Deleted,

    Enabled,

    Disabled,

    TargetingChanged,

    RolloutChanged
}
