namespace GameGuild.Identity.Authentication;

/// <summary>
///     DTO for sign-in response
/// </summary>
public class SignInResponse
{
    /// <summary>
    ///     Whether sign-in was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    ///     Response message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    ///     JWT access token
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    ///     Refresh token
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    ///     Backward compatible field: originally represented refresh token expiry (or conflated); prefer using AccessTokenExpiresAt / RefreshTokenExpiresAt.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    ///     When the access token expires (short-lived)
    /// </summary>
    public DateTime AccessTokenExpiresAt { get; set; }

    /// <summary>
    ///     When the refresh token expires (long-lived)
    /// </summary>
    public DateTime RefreshTokenExpiresAt { get; set; }

    /// <summary>
    ///     Expiration in seconds
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    ///     User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    ///     User email
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    ///     Session ID
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    ///     Temporary token for MFA flows
    /// </summary>
    public string? TempToken { get; set; }

    /// <summary>
    ///     MFA token
    /// </summary>
    public string? MfaToken { get; set; }

    /// <summary>
    ///     User information
    /// </summary>
    public UserDto User { get; set; } = new UserDto();

    /// <summary>
    ///     Current tenant ID
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    ///     List of tenants the user has access to
    /// </summary>
    public IEnumerable<TenantInfo>? AvailableTenants { get; set; }

    /// <summary>
    ///     Whether MFA is required
    /// </summary>
    public bool RequiresMfa { get; set; }

    /// <summary>
    ///     MFA session ID if MFA is required
    /// </summary>
    public string? MfaSessionId { get; set; }
}
