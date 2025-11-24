using System.ComponentModel.DataAnnotations;

namespace GameGuild.Authentication.DTOs;

/// <summary>
///     Request for Google ID token sign-in
/// </summary>
public class GoogleIdTokenRequestDto
{
    [Required]
    public string IdToken { get; set; } = string.Empty;

    public Guid? TenantId { get; set; }

    public string? DeviceFingerprint { get; set; }
}
