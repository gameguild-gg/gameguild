namespace GameGuild.Users.RequestModels;

/// <summary>
///     Request model for updating a user via API
/// </summary>
/// <param name="Name">Updated user name</param>
/// <param name="PhoneNumber">Updated phone number</param>
public record UpdateUserRequest(string Name, string? PhoneNumber = null);
