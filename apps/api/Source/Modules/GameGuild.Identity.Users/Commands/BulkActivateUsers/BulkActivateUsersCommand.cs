using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command to activate multiple users
/// </summary>
/// <param name="UserIds">Collection of user IDs to activate</param>
public record BulkActivateUsersCommand(IEnumerable<Guid> UserIds) : ICommand<BulkActivateUsersResponse>;
