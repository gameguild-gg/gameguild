namespace GameGuild.Authentication.Enums;

/// <summary>
///     Specific reasons why an authentication attempt failed.
///     Used for detailed logging and user feedback.
/// </summary>
public enum AuthenticationFailureReason
{
    /// <summary>
    ///     User credentials (password) were incorrect.
    /// </summary>
    InvalidCredentials,

    /// <summary>
    ///     User account does not exist.
    /// </summary>
    UserNotFound,

    /// <summary>
    ///     User account is locked due to too many failed attempts.
    /// </summary>
    AccountLocked,

    /// <summary>
    ///     User account has been disabled or suspended.
    /// </summary>
    AccountDisabled,

    /// <summary>
    ///     Email address has not been verified.
    /// </summary>
    EmailNotVerified,

    /// <summary>
    ///     MFA verification failed or required but not provided.
    /// </summary>
    MfaRequired,

    /// <summary>
    ///     MFA code was invalid or expired.
    /// </summary>
    InvalidMfaCode,

    /// <summary>
    ///     Token (refresh, reset, etc.) is expired.
    /// </summary>
    TokenExpired,

    /// <summary>
    ///     Token is invalid or malformed.
    /// </summary>
    InvalidToken,

    /// <summary>
    ///     Token has been revoked.
    /// </summary>
    TokenRevoked,

    /// <summary>
    ///     Session is invalid or expired.
    /// </summary>
    InvalidSession,

    /// <summary>
    ///     User is not authorized for the requested tenant.
    /// </summary>
    UnauthorizedTenant,

    /// <summary>
    ///     OAuth provider authentication failed.
    /// </summary>
    OAuthProviderError,

    /// <summary>
    ///     Web3 signature verification failed.
    /// </summary>
    InvalidWeb3Signature,

    /// <summary>
    ///     Request was blocked due to rate limiting.
    /// </summary>
    RateLimitExceeded,

    /// <summary>
    ///     Request was throttled to protect against enumeration attacks.
    /// </summary>
    Throttled,

    /// <summary>
    ///     Suspicious activity detected.
    /// </summary>
    SuspiciousActivity,

    /// <summary>
    ///     Anomalous activity detected by behavioral analysis.
    /// </summary>
    AnomalousActivity,

    /// <summary>
    ///     System or internal error occurred during authentication.
    /// </summary>
    SystemError,

    /// <summary>
    ///     Unknown or internal error occurred.
    /// </summary>
    Unknown
}
