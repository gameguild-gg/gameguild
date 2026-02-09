namespace GameGuild.Identity.Users;

/// <summary>
///     Request model for updating a user via API
/// </summary>
/// <param name="Name">Updated user name</param>
/// <param name="PhoneNumber">Updated phone number</param>
public sealed record UpdateUserRequest(string Name, string? PhoneNumber = null);
