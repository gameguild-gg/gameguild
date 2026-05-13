namespace GameGuild.Identity.Users;

/// <summary>
///     API request item for updating a user in bulk operations
/// </summary>
/// <param name="UserId">User's unique identifier</param>
/// <param name="Name">Updated user name</param>
/// <param name="PhoneNumber">Updated phone number</param>
public record UpdateUserRequestItem(Guid UserId, string Name, string? PhoneNumber = null);
