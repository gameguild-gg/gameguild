using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Request model for bulk restore users operation
/// </summary>
/// <param name="UserIds">Collection of user IDs to restore</param>
public sealed record BulkRestoreUsersRequest(IEnumerable<Guid> UserIds);

/// <summary>
///     Response of bulk restore users operation
/// </summary>
/// <param name="RestoredUsers">Successfully restored users</param>
/// <param name="FailedUserIds">User IDs that failed to restore</param>
public sealed record BulkRestoreUsersResponse(IEnumerable<UserDto> RestoredUsers, IEnumerable<Guid> FailedUserIds);

/// <summary>
///     Command to restore multiple soft-deleted users in bulk
/// </summary>
/// <param name="UserIds">Collection of user IDs to restore</param>
public sealed record BulkRestoreUsersCommand(IEnumerable<Guid> UserIds) : ICommand<BulkRestoreUsersResponse>;
