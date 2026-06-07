using System.ComponentModel.DataAnnotations;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Request for Google ID token sign-in
/// </summary>
public class GoogleIdTokenRequestDto
{
    [Required]
    public string IdToken { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
}

/// <summary>
/// Type alias for compatibility
/// </summary>
public class GoogleIdTokenRequest : GoogleIdTokenRequestDto
{
    public string? DeviceFingerprint { get; set; }
}
