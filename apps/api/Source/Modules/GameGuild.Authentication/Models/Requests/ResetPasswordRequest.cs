using System.ComponentModel.DataAnnotations;

namespace GameGuild.Authentication.Models.Requests;

/// <summary>
///     Request to reset password
/// </summary>
public abstract class ResetPasswordRequest
{
    [Required(ErrorMessage = "Reset token is required")]
    [MaxLength(500)]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    [MaxLength(100)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password confirmation is required")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
    [MaxLength(100)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
