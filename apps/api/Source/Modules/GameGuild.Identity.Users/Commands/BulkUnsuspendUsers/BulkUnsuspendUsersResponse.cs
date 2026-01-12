
namespace GameGuild.Identity.Users;

/// <summary>
///     Response of bulk unsuspend users operation
/// </summary>
/// <param name="UnsuspendedUsers">Successfully unsuspended users</param>
/// <param name="FailedUserIds">User IDs that failed to unsuspend</param>
public record BulkUnsuspendUsersResponse(IEnumerable<UserDto> UnsuspendedUsers, IEnumerable<Guid> FailedUserIds);
