
namespace GameGuild.Identity.Users;

/// <summary>
///     Response of bulk activate users operation
/// </summary>
/// <param name="ActivatedUsers">Successfully activated users</param>
/// <param name="FailedUserIds">User IDs that failed to activate</param>
public sealed record BulkActivateUsersResponse(IEnumerable<UserDto> ActivatedUsers, IEnumerable<Guid> FailedUserIds);
