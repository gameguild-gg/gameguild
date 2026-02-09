using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command to restore a soft-deleted user
/// </summary>
/// <param name="UserId">The ID of the user to restore</param>
public sealed record RestoreUserCommand(Guid UserId) : ICommand<UserDto>;
