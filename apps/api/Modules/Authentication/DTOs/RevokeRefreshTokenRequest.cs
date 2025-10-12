namespace GameGuild.Modules.Authentication;

/// <summary> Request DTO for revoking authentication tokens </summary>
public class RevokeRefreshTokenRequest
{
    /// <summary> The refresh token to revoke </summary>
    public string RefreshToken { get; set; } = string.Empty;
}
