using System.ComponentModel.DataAnnotations;

namespace GameGuild.Authentication.DTOs;

/// <summary>
///     Request to send phone verification code
/// </summary>
public class SendPhoneVerificationRequest
{
    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;
}
