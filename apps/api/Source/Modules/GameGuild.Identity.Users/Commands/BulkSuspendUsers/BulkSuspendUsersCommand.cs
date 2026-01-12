using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command to suspend multiple users
/// </summary>
/// <param name="UserIds">Collection of user IDs to suspend</param>
public record BulkSuspendUsersCommand(IEnumerable<Guid> UserIds) : ICommand<BulkSuspendUsersResponse>;
