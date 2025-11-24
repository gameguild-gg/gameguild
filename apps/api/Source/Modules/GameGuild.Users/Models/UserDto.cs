namespace GameGuild.Users.Models;

/// <summary>
///     User data transfer object
/// </summary>
/// <param name="Id">User unique identifier</param>
/// <param name="Email">User email address</param>
/// <param name="Name">User full name</param>
/// <param name="CreatedAt">When the user was created</param>
/// <param name="UpdatedAt">When the user was last updated</param>
/// <param name="IsActive">Whether the user is active</param>
/// <param name="PhoneNumber">Optional phone number</param>
/// <param name="LastSeenAt">When the user was last seen/logged in</param>
public record UserDto(Guid Id, string Email, string Name, DateTime CreatedAt, DateTime? UpdatedAt, bool IsActive = true, string? PhoneNumber = null, DateTime? LastSeenAt = null);
