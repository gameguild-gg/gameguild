using GameGuild.CQRS;
using GameGuild.Resources;

namespace GameGuild.Features;

/// <summary>
///     Command to create a new feature
/// </summary>
[RequiresQuota(ResourceUsageType.FeatureFlags, Source = "CreateFeature")]
public record CreateFeatureCommand(string Key, string Name, string? Description = null) : ICommand<Guid>;
