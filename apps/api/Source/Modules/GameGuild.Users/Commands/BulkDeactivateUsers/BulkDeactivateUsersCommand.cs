using GameGuild.CQRS;

namespace GameGuild.Users.Commands;

/// <summary>
///     Command to deactivate multiple users
/// </summary>
/// <param name="UserIds">Collection of user IDs to deactivate</param>
public record BulkDeactivateUsersCommand(IEnumerable<Guid> UserIds) : ICommand<BulkDeactivateUsersResult>;
