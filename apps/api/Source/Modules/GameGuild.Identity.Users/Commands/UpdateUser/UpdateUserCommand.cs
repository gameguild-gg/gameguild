using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command to update an existing user
/// </summary>
/// <param name="UserId">User's unique identifier</param>
/// <param name="Name">Updated user name</param>
/// <param name="PhoneNumber">Updated phone number</param>
public record UpdateUserCommand(Guid UserId, string Name, string? PhoneNumber = null) : ICommand<UserDto>;
