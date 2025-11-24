using GameGuild.CQRS;

namespace GameGuild.Users.Commands;

/// <summary>
///     Request model for bulk purge users operation
/// </summary>
/// <param name="UserIds">Collection of user IDs to permanently delete</param>
/// <param name="Strategy">The purge strategy to use</param>
public record BulkPurgeUsersRequest(IEnumerable<Guid> UserIds, PurgeStrategy Strategy = PurgeStrategy.GracePeriod);

/// <summary>
///     Command to permanently delete multiple users in bulk
/// </summary>
/// <param name="UserIds">Collection of user IDs to purge</param>
/// <param name="Strategy">The purge strategy to use</param>
public record BulkPurgeUsersCommand(IEnumerable<Guid> UserIds, PurgeStrategy Strategy) : ICommand;
