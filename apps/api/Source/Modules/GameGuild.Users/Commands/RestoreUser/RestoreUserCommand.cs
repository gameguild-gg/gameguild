using GameGuild.CQRS;
using GameGuild.Users.Models;

namespace GameGuild.Users.Commands;

/// <summary>
///     Command to restore a soft-deleted user
/// </summary>
/// <param name="UserId">The ID of the user to restore</param>
public record RestoreUserCommand(Guid UserId) : ICommand<UserDto>;
