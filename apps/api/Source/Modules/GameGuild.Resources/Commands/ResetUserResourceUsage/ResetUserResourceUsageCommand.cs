using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Command to reset resource usage for a user
/// </summary>
/// <param name="UserId">User unique identifier</param>
/// <param name="ResourceUsageType">Type of resource usage to reset</param>
public sealed record ResetUserResourceUsageCommand(Guid UserId, ResourceUsageType ResourceUsageType) : ICommand;
