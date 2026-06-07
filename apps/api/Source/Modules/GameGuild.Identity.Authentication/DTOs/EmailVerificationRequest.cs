using System.ComponentModel.DataAnnotations;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Email verification request
/// </summary>
public class EmailVerificationRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;
}
