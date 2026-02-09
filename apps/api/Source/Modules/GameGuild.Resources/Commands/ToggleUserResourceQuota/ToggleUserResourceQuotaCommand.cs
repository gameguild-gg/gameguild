using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Command to toggle a resource quota activation status for a user
/// </summary>
/// <param name="UserId">User unique identifier</param>
/// <param name="Type">Type of resource quota to toggle</param>
/// <param name="IsActive">Whether the quota should be active</param>
public sealed record ToggleUserResourceQuotaCommand(Guid UserId, ResourceUsageType Type, bool IsActive) : ICommand;
