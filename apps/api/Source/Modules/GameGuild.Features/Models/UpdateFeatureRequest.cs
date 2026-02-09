namespace GameGuild.Features;

/// <summary>
///     Request model for updating an existing feature flag.
///     All properties are optional; only provided values will be updated.
/// </summary>
public sealed record UpdateFeatureRequest(string? Name, string? Description, bool? IsEnabled, int? RolloutPercentage, string? EnabledValue, string? DefaultValue);
