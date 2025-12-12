using GameGuild.CQRS;

namespace GameGuild.Users.Commands;

/// <summary>
///     Command to suspend multiple users
/// </summary>
/// <param name="UserIds">Collection of user IDs to suspend</param>
public record BulkSuspendUsersCommand(IEnumerable<Guid> UserIds) : ICommand<BulkSuspendUsersResult>;
