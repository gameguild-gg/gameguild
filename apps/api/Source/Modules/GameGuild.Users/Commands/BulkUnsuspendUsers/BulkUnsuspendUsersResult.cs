using GameGuild.Users.Models;

namespace GameGuild.Users.Commands;

/// <summary>
///     Result of bulk unsuspend users operation
/// </summary>
/// <param name="UnsuspendedUsers">Successfully unsuspended users</param>
/// <param name="FailedUserIds">User IDs that failed to unsuspend</param>
public record BulkUnsuspendUsersResult(IEnumerable<UserDto> UnsuspendedUsers, IEnumerable<Guid> FailedUserIds);
