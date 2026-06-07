using System.ComponentModel.DataAnnotations;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Request for Google OAuth sign-in
/// </summary>
public class GoogleSignInRequestDto
{
    [Required]
    public string Code { get; set; } = string.Empty;

    [Required]
    public string RedirectUri { get; set; } = string.Empty;

    public string? State { get; set; }

    public Guid? TenantId { get; set; }

    public string? DeviceFingerprint { get; set; }
}
