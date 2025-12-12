using System.ComponentModel.DataAnnotations;

namespace GameGuild.Authentication.Models.Requests;

/// <summary>
///     Request for token refresh
/// </summary>
public class RefreshTokenRequest
{
    [Required(ErrorMessage = "Refresh token is required")]
    [MaxLength(500)]
    public string RefreshToken { get; set; } = string.Empty;

    [MaxLength(45)]
    public string? IpAddress { get; set; }
}
