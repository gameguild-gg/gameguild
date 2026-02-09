
namespace GameGuild.Identity.Users;

/// <summary>
///     Response of bulk suspend users operation
/// </summary>
/// <param name="SuspendedUsers">Successfully suspended users</param>
/// <param name="FailedUserIds">User IDs that failed to suspend</param>
public sealed record BulkSuspendUsersResponse(IEnumerable<UserDto> SuspendedUsers, IEnumerable<Guid> FailedUserIds);
