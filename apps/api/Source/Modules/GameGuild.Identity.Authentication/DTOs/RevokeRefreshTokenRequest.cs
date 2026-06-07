using System.ComponentModel.DataAnnotations;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Request for revoking a refresh token
/// </summary>
public class RevokeRefreshTokenRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;

    public string? IpAddress { get; set; }

    public string? Reason { get; set; }
}
