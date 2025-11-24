using System.ComponentModel.DataAnnotations;

namespace GameGuild.Authentication.Models.Requests;

/// <summary>
///     Request to verify email
/// </summary>
public abstract class EmailVerificationRequest
{
    [Required(ErrorMessage = "Verification token is required")]
    [MaxLength(500)]
    public string Token { get; set; } = string.Empty;
}
