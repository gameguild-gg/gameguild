using GameGuild.CQRS;

namespace GameGuild.Users.Commands;

/// <summary>
///     Command to unsuspend multiple users
/// </summary>
/// <param name="UserIds">Collection of user IDs to unsuspend</param>
public record BulkUnsuspendUsersCommand(IEnumerable<Guid> UserIds) : ICommand<BulkUnsuspendUsersResult>;
