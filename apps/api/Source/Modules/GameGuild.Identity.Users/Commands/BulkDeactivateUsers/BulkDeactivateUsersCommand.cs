using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command to deactivate multiple users
/// </summary>
/// <param name="UserIds">Collection of user IDs to deactivate</param>
public sealed record BulkDeactivateUsersCommand(IEnumerable<Guid> UserIds) : ICommand<BulkDeactivateUsersResponse>;
