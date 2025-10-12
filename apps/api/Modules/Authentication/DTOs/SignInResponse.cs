using GameGuild.Modules.Users;

namespace GameGuild.Modules.Authentication;

/// <summary> DTO for sign-in response </summary>
public class SignInResponse
{
    /// <summary> JWT access token </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary> Refresh token </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary> Backward compatible field: originally represented refresh token expiry (or conflated); prefer using AccessTokenExpiresAt / RefreshTokenExpiresAt. </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary> When the access token expires (short-lived) </summary>
    public DateTime AccessTokenExpiresAt { get; set; }

    /// <summary> When the refresh token expires (long-lived) </summary>
    public DateTime RefreshTokenExpiresAt { get; set; }

    /// <summary> User information </summary>
    public UserDto User { get; set; } = new UserDto();

    /// <summary> Current tenant ID </summary>
    public Guid? TenantId { get; set; }

    /// <summary> List of tenants the user has access to </summary>
    public IEnumerable<TenantInfo>? AvailableTenants { get; set; }
}
