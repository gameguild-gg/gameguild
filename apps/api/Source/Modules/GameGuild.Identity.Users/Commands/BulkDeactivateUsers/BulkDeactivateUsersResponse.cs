
namespace GameGuild.Identity.Users;

/// <summary>
///     Response of bulk deactivate users operation
/// </summary>
/// <param name="DeactivatedUsers">Successfully deactivated users</param>
/// <param name="FailedUserIds">User IDs that failed to deactivate</param>
public sealed record BulkDeactivateUsersResponse(IEnumerable<UserDto> DeactivatedUsers, IEnumerable<Guid> FailedUserIds);
