
namespace GameGuild.Identity.Users;

/// <summary>
///     Response of bulk deactivate users operation
/// </summary>
/// <param name="DeactivatedUsers">Successfully deactivated users</param>
/// <param name="FailedUserIds">User IDs that failed to deactivate</param>
public record BulkDeactivateUsersResponse(IEnumerable<UserDto> DeactivatedUsers, IEnumerable<Guid> FailedUserIds);
