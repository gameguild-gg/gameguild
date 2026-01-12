using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command to delete a user
/// </summary>
/// <param name="UserId">User's unique identifier</param>
public record DeleteUserCommand(Guid UserId) : ICommand;
