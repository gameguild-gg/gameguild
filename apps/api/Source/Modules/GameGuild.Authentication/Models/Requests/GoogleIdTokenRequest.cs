using System.ComponentModel.DataAnnotations;

namespace GameGuild.Authentication.Models.Requests;

/// <summary>
///     Request for Google ID token sign-in
/// </summary>
public class GoogleIdTokenRequest
{
    [Required(ErrorMessage = "ID token is required")]
    [MaxLength(2000)]
    public string IdToken { get; set; } = string.Empty;

    public Guid? TenantId { get; set; }

    [MaxLength(256)]
    public string? DeviceFingerprint { get; set; }
}
