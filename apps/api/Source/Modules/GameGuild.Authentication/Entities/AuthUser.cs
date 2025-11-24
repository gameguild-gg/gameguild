namespace GameGuild.Authentication.Entities;

/// <summary>
///     Represents an authenticated user in the Authentication module.
///     This is separate from the User module's User entity.
/// </summary>
public class AuthUser
{
    public Guid Id { get; set; }

    public required string Email { get; set; }

    public string? Username { get; set; }

    public required string PasswordHash { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
