using System.ComponentModel.DataAnnotations;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Request to send phone verification code
/// </summary>
public class SendPhoneVerificationRequest
{
    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;
}
