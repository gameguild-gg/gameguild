using GameGuild.CQRS;

namespace GameGuild.Features.Commands;

/// <summary>
///     Command to update an existing feature flag
/// </summary>
public record UpdateFeatureFlagCommand(
    Guid Id,
    string? Name = null,
    string? Description = null,
    bool? IsEnabled = null,
    int? RolloutPercentage = null,
    string? EnabledValue = null,
    string? DefaultValue = null
) : ICommand<bool>;
