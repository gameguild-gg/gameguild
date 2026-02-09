using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Command to cleanup orphaned resources
/// </summary>
/// <param name="DryRun">If true, only report what would be cleaned up without actually deleting</param>
/// <param name="ResourceTypes">Optional list of resource types to clean up. If null, all types are considered.</param>
public sealed record CleanupOrphanedResourcesCommand(bool DryRun = true, List<ResourceUsageType>? ResourceTypes = null) : ICommand<int>;
