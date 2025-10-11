namespace GameGuild.Modules.Authentication;

/// <summary> Response DTO with refreshed authentication tokens </summary>
public class RefreshTokenResponse
{
    /// <summary> New JWT access token </summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary> New refresh token </summary>
    public string RefreshToken { get; init; } = string.Empty;

    /// <summary> Backward compatible combined expiry (was refresh token expiry). Prefer using AccessTokenExpiresAt/RefreshTokenExpiresAt. </summary>
    public DateTime ExpiresAt { get; init; }

    /// <summary> Access token expiry (short-lived) </summary>
    public DateTime AccessTokenExpiresAt { get; init; }

    /// <summary> Refresh token expiry (long-lived) </summary>
    public DateTime RefreshTokenExpiresAt { get; init; }

    /// <summary> Tenant ID associated with this token (if any) </summary>
    public Guid? TenantId { get; init; }
}
