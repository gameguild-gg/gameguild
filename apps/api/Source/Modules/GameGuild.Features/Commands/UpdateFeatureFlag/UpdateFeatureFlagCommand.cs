using GameGuild.CQRS;

namespace GameGuild.Features;

/// <summary>
///     Command to update an existing feature flag
/// </summary>
public sealed record UpdateFeatureFlagCommand(
    Guid Id,
    string? Name = null,
    string? Description = null,
    bool? IsEnabled = null,
    int? RolloutPercentage = null,
    string? EnabledValue = null,
    string? DefaultValue = null
) : ICommand<bool>;
