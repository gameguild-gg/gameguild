using System.ComponentModel.DataAnnotations;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Links a GameGuild user to an external identity provider (e.g. Google, GitHub).
///     One row per (Provider, ProviderKey) pair — the unique index enforces that a single
///     external identity can never map to more than one GameGuild user.
/// </summary>
public class ExternalLogin
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    [Required]
    [MaxLength(64)]
    public string Provider { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string ProviderKey { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
