using GameGuild.CQRS;
using GameGuild.Users.Models;

namespace GameGuild.Users.Commands;

/// <summary>
///     Command to deactivate a user
/// </summary>
/// <param name="UserId">User's unique identifier</param>
public record DeactivateUserCommand(Guid UserId) : ICommand<UserDto>;
