using GameGuild.CQRS;
using GameGuild.Users.Models;

namespace GameGuild.Users.Commands;

/// <summary>
///     Request model for bulk restore users operation
/// </summary>
/// <param name="UserIds">Collection of user IDs to restore</param>
public record BulkRestoreUsersRequest(IEnumerable<Guid> UserIds);

/// <summary>
///     Result of bulk restore users operation
/// </summary>
/// <param name="RestoredUsers">Successfully restored users</param>
/// <param name="FailedUserIds">User IDs that failed to restore</param>
public record BulkRestoreUsersResult(IEnumerable<UserDto> RestoredUsers, IEnumerable<Guid> FailedUserIds);

/// <summary>
///     Command to restore multiple soft-deleted users in bulk
/// </summary>
/// <param name="UserIds">Collection of user IDs to restore</param>
public record BulkRestoreUsersCommand(IEnumerable<Guid> UserIds) : ICommand<BulkRestoreUsersResult>;
