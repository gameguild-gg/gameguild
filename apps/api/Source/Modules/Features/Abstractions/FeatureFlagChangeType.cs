namespace GameGuild.Modules.Features.Abstractions;

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

