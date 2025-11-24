using GameGuild.Users.Models;

namespace GameGuild.Users.Commands;

/// <summary>
///     Result of bulk deactivate users operation
/// </summary>
/// <param name="DeactivatedUsers">Successfully deactivated users</param>
/// <param name="FailedUserIds">User IDs that failed to deactivate</param>
public record BulkDeactivateUsersResult(IEnumerable<UserDto> DeactivatedUsers, IEnumerable<Guid> FailedUserIds);
