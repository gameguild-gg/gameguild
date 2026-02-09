namespace GameGuild.Identity.Users;

/// <summary>
///     Response indicating whether a user exists
/// </summary>
/// <param name="Exists">Whether the user exists</param>
/// <param name="Email">The email that was checked</param>
/// <param name="UserId">The user ID if the user exists</param>
public sealed record UserExistsResponse(bool Exists, string Email, Guid? UserId = null);
