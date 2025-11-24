namespace GameGuild.Authentication.DTOs;

/// <summary>
///     Response DTO for refresh token operations
/// </summary>
public class RefreshTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public UserDto User { get; set; } = new UserDto();
}
