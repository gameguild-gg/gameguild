using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command to activate a user
/// </summary>
/// <param name="UserId">User's unique identifier</param>
public record ActivateUserCommand(Guid UserId) : ICommand<UserDto>;
