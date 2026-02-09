using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Command to delete a resource quota for a user
/// </summary>
/// <param name="UserId">User unique identifier</param>
/// <param name="Type">Type of resource quota to delete</param>
public sealed record DeleteUserResourceQuotaCommand(Guid UserId, ResourceUsageType Type) : ICommand;
