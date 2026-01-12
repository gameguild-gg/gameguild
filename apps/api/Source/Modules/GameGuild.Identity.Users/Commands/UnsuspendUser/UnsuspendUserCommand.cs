using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command to unsuspend a user account
/// </summary>
/// <param name="UserId">User's unique identifier</param>
public record UnsuspendUserCommand(Guid UserId) : ICommand<UserDto>;
