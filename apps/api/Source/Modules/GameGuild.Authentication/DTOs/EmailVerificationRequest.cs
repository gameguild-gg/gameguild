using System.ComponentModel.DataAnnotations;

namespace GameGuild.Authentication.DTOs;

/// <summary>
///     Email verification request
/// </summary>
public class EmailVerificationRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;
}
