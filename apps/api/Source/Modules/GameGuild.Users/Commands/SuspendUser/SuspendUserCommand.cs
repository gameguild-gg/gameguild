using GameGuild.CQRS;
using GameGuild.Users.Models;

namespace GameGuild.Users.Commands;

/// <summary>
///     Command to suspend a user account
/// </summary>
/// <param name="UserId">User's unique identifier</param>
public record SuspendUserCommand(Guid UserId) : ICommand<UserDto>;
