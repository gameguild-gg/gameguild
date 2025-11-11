using System.ComponentModel.DataAnnotations;

namespace GameGuild.Authentication.Models.Requests;

/// <summary>
///     Request for OAuth sign-in
/// </summary>
public abstract class OAuthSignInRequest
{
    [Required(ErrorMessage = "Access token is required")]
    [MaxLength(2000)]
    public string AccessToken { get; set; } = string.Empty;

    public Guid? TenantId { get; set; }

    [MaxLength(256)]
    public string? DeviceFingerprint { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }
}
