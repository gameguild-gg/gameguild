namespace GameGuild.Authentication.Models.Responses;

/// <summary>
///     Response for token refresh operations
/// </summary>
public abstract class TokenRefreshResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string AccessToken { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public int ExpiresIn { get; set; }
}
