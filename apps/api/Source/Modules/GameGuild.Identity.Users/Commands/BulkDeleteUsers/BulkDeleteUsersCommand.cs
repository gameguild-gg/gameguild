using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command to delete multiple users
/// </summary>
/// <param name="UserIds">Collection of user IDs to delete</param>
public record BulkDeleteUsersCommand(IEnumerable<Guid> UserIds) : ICommand;
