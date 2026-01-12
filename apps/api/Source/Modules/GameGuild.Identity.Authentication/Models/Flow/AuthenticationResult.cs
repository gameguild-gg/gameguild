namespace GameGuild.Identity.Authentication;

/// <summary>
///     Final result of a successful authentication.
/// </summary>
public abstract class AuthenticationResult
{
    /// <summary>
    ///     Whether authentication was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    ///     User ID.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    ///     JWT access token.
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    ///     Refresh token.
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    ///     When the access token expires.
    /// </summary>
    public DateTime? TokenExpiresAt { get; set; }

    /// <summary>
    ///     Session ID.
    /// </summary>
    public Guid? SessionId { get; set; }

    /// <summary>
    ///     User profile information.
    /// </summary>
    public object? UserProfile { get; set; }

    /// <summary>
    ///     Whether MFA is enabled for this user.
    /// </summary>
    public bool MfaEnabled { get; set; }

    /// <summary>
    ///     Whether email is verified.
    /// </summary>
    public bool EmailVerified { get; set; }

    /// <summary>
    ///     Whether this device was trusted.
    /// </summary>
    public bool DeviceTrusted { get; set; }

    /// <summary>
    ///     Risk score for this authentication.
    /// </summary>
    public double? RiskScore { get; set; }

    /// <summary>
    ///     Failure reason if authentication failed.
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    ///     Additional result metadata.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}
