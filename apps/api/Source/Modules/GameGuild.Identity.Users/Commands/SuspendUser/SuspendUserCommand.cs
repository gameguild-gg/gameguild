using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command to suspend a user account
/// </summary>
/// <param name="UserId">User's unique identifier</param>
public record SuspendUserCommand(Guid UserId) : ICommand<UserDto>;
