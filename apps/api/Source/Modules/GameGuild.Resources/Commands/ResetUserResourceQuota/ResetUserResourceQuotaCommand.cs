using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Command to reset a resource quota usage for a user
/// </summary>
/// <param name="UserId">User unique identifier</param>
/// <param name="Type">Type of resource quota to reset</param>
public sealed record ResetUserResourceQuotaCommand(Guid UserId, ResourceUsageType Type) : ICommand;
