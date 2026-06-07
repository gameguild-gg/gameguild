using System.ComponentModel.DataAnnotations;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Password reset request
/// </summary>
public class PasswordResetRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Type alias for compatibility
/// </summary>
public class PasswordResetRequest : PasswordResetRequestDto
{
}
