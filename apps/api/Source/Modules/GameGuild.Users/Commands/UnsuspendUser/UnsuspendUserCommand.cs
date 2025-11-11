using GameGuild.CQRS;
using GameGuild.Users.Models;

namespace GameGuild.Users.Commands;

/// <summary>
///     Command to unsuspend a user account
/// </summary>
/// <param name="UserId">User's unique identifier</param>
public record UnsuspendUserCommand(Guid UserId) : ICommand<UserDto>;
