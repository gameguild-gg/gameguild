using GameGuild.CQRS;

namespace GameGuild.Users.Commands;

/// <summary>
///     Command to create multiple users in bulk
/// </summary>
/// <param name="Users">Collection of user creation data</param>
public record BulkCreateUsersCommand(IEnumerable<CreateUserRequestItem> Users) : ICommand<BulkCreateUsersResult>;
