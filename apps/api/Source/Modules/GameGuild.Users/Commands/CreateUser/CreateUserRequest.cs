namespace GameGuild.Users.Commands;

/// <summary>
///     Request model for creating a user via API
/// </summary>
/// <param name="Email">User's email address</param>
/// <param name="Name">User's full name</param>
/// <param name="PhoneNumber">Optional phone number</param>
public record CreateUserRequest(string Email, string Name, string? PhoneNumber = null);
