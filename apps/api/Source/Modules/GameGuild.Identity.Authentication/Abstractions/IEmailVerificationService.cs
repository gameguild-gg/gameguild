namespace GameGuild.Identity.Authentication;

/// <summary>
///     Service for handling email verification operations.
///     Manages verification token generation, validation, and email sending coordination.
///     Cache keys contain token digests; consumption writes a consumed sentinel before removal,
///     so a token claims one-time use by read-then-remove. Verification and password-reset tokens
///     live in <see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache"/> (Redis in
///     production, the host's distributed-memory cache in single-node/dev) so restarts and
///     multi-node deployments share one durable token space. That abstraction has no atomic
///     compare-and-delete, so simultaneous cross-instance presentations of one token are bounded
///     by the remove protocol rather than an in-process interlock.
/// </summary>
public interface IEmailVerificationService
{
    /// <summary>
    ///     Generates a secure verification token for email confirmation.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="email">The email address to verify</param>
    /// <returns>Verification token</returns>
    Task<string> GenerateVerificationTokenAsync(Guid userId, string email);

    /// <summary>
    ///     Generates a secure password reset token.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="email">The user's email address</param>
    /// <returns>Password reset token</returns>
    Task<string> GeneratePasswordResetTokenAsync(Guid userId, string email);

    /// <summary>
    ///     Generates a one-time magic-link sign-in token.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="email">The user's email address</param>
    /// <returns>Magic-link token</returns>
    Task<string> GenerateMagicLinkTokenAsync(Guid userId, string email);

    /// <summary>
    ///     Sends a verification email to the user.
    /// </summary>
    /// <param name="email">The email address to send to</param>
    /// <param name="token">The verification token</param>
    /// <param name="userName">Optional user name for personalization</param>
    Task SendVerificationEmailAsync(string email, string token, string? userName = null);

    /// <summary>
    ///     Verifies an email using the provided token.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="token">The verification token</param>
    /// <returns>True if verification is successful</returns>
    Task<bool> VerifyEmailTokenAsync(Guid userId, string token);

    /// <summary>
    ///     Verifies and consumes an email verification token without requiring the caller to know the user ID.
    /// </summary>
    /// <param name="token">The verification token</param>
    /// <returns>Validated token information when successful</returns>
    Task<TokenValidationResult> VerifyEmailTokenAsync(string token);

    /// <summary>
    ///     Verifies and consumes a password reset token.
    /// </summary>
    /// <param name="token">The reset token</param>
    /// <returns>Validated token information when successful</returns>
    Task<TokenValidationResult> VerifyPasswordResetTokenAsync(string token);

    /// <summary>
    ///     Verifies and consumes a magic-link sign-in token.
    /// </summary>
    /// <param name="token">The magic-link token</param>
    /// <returns>Validated token information when successful</returns>
    Task<TokenValidationResult> VerifyMagicLinkTokenAsync(string token);

    /// <summary>
    ///     Checks if an email address is already verified for a user.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>True if email is verified</returns>
    Task<bool> IsEmailVerifiedAsync(Guid userId);

    /// <summary>
    ///     Resends a verification email if the previous one expired or wasn't received.
    ///     Implements rate limiting to prevent abuse.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="email">The email address</param>
    /// <returns>True if resend was successful, false if rate limited</returns>
    Task<bool> ResendVerificationEmailAsync(Guid userId, string email);

    /// <summary>
    ///     Checks the shared per-(user, address) resend cooldown without sending anything.
    ///     Every verification-email dispatch path must consult this gate so the cooldown
    ///     cannot be bypassed (issue #1197). Fails open when the cooldown store is unavailable.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="email">The email address</param>
    /// <returns>True when a verification email may be sent now, false while the cooldown window is active</returns>
    Task<bool> CanResendVerificationEmailAsync(Guid userId, string email);

    /// <summary>
    ///     Stamps the shared per-(user, address) resend cooldown after a successful
    ///     verification-email delivery (issue #1197). Best-effort; must not throw.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="email">The email address</param>
    Task MarkVerificationEmailSentAsync(Guid userId, string email);

    /// <summary>
    ///     Checks if a verification token is still valid (not expired).
    /// </summary>
    /// <param name="token">The verification token</param>
    /// <returns>True if token is valid and not expired</returns>
    Task<bool> IsTokenValidAsync(string token);
}

/// <summary>
///     Result of consuming an email verification or password reset token.
/// </summary>
public sealed record TokenValidationResult(
    bool Success,
    Guid? UserId = null,
    string? Email = null,
    string? FailureReason = null)
{
    public static TokenValidationResult Failed(string reason) => new(false, null, null, reason);
}
