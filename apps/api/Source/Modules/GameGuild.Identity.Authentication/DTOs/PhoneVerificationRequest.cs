using System.ComponentModel.DataAnnotations;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Request for phone number verification via SMS
/// </summary>
public class PhoneVerificationRequest
{
    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    public string VerificationCode { get; set; } = string.Empty;
}
