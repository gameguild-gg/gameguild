using System.ComponentModel.DataAnnotations;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Represents an authenticated user in the Authentication module.
///     This is separate from the User module's User entity.
/// </summary>
public class AuthUser
{
    public Guid Id { get; set; }

    [MaxLength(256)]
    public required string Email { get; set; }

    [MaxLength(256)]
    public string? Username { get; set; }

    [MaxLength(512)]
    public required string PasswordHash { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
