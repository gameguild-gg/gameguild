using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command to deactivate a user
/// </summary>
/// <param name="UserId">User's unique identifier</param>
public sealed record DeactivateUserCommand(Guid UserId) : ICommand<UserDto>;
