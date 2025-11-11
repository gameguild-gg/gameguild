using System.ComponentModel.DataAnnotations;

namespace GameGuild.Authentication.Models.Requests;

/// <summary>
///     Request for local sign-in with email/password
/// </summary>
public class LocalSignInRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    public Guid? TenantId { get; set; }

    [MaxLength(256)]
    public string? DeviceFingerprint { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    public bool RememberMe { get; set; }

    /// <summary>
    ///     Alias for Email to support polymorphic sign-in scenarios
    /// </summary>
    public string EmailOrUsername { get => Email; }
}
