using GameGuild.Users.Models;

namespace GameGuild.Users.Commands;

/// <summary>
///     Result of bulk activate users operation
/// </summary>
/// <param name="ActivatedUsers">Successfully activated users</param>
/// <param name="FailedUserIds">User IDs that failed to activate</param>
public record BulkActivateUsersResult(IEnumerable<UserDto> ActivatedUsers, IEnumerable<Guid> FailedUserIds);
