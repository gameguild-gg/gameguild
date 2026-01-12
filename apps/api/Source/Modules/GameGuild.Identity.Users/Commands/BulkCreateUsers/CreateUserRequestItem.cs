namespace GameGuild.Identity.Users;

/// <summary>
///     API request item for creating a user in bulk operations
/// </summary>
/// <param name="Email">User's email address</param>
/// <param name="Name">User's full name</param>
/// <param name="PhoneNumber">Optional phone number</param>
public record CreateUserRequestItem(string Email, string Name, string? PhoneNumber = null);
