using GameGuild.Users.Models;

namespace GameGuild.Users.Commands;

/// <summary>
///     Result of bulk suspend users operation
/// </summary>
/// <param name="SuspendedUsers">Successfully suspended users</param>
/// <param name="FailedUserIds">User IDs that failed to suspend</param>
public record BulkSuspendUsersResult(IEnumerable<UserDto> SuspendedUsers, IEnumerable<Guid> FailedUserIds);
