using GameGuild.CQRS;
using GameGuild.Users.Models;

namespace GameGuild.Users.Commands;

/// <summary>
///     Command to activate a user
/// </summary>
/// <param name="UserId">User's unique identifier</param>
public record ActivateUserCommand(Guid UserId) : ICommand<UserDto>;
