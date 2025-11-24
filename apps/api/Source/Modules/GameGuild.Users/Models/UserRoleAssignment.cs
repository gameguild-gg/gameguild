namespace GameGuild.Users.Models;

/// <summary>
///     Request model for assigning roles to users in bulk
/// </summary>
/// <param name="UserId">User's unique identifier</param>
/// <param name="Role">Role to assign</param>
public record UserRoleAssignment(Guid UserId, string Role);
