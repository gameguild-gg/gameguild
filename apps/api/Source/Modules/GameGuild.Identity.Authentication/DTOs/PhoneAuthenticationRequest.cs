using System.ComponentModel.DataAnnotations;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Request for phone number authentication
/// </summary>
public class PhoneAuthenticationRequest
{
    /// <summary>
    ///     Phone number in international format (e.g., +1234567890)
    /// </summary>
    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    ///     Password for authentication
    /// </summary>
    [Required]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    ///     Optional tenant context
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    ///     Optional device fingerprint
    /// </summary>
    public string? DeviceFingerprint { get; set; }
}
