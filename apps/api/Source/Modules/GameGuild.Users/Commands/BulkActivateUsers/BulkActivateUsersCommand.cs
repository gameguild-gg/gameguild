using GameGuild.CQRS;

namespace GameGuild.Users.Commands;

/// <summary>
///     Command to activate multiple users
/// </summary>
/// <param name="UserIds">Collection of user IDs to activate</param>
public record BulkActivateUsersCommand(IEnumerable<Guid> UserIds) : ICommand<BulkActivateUsersResult>;
