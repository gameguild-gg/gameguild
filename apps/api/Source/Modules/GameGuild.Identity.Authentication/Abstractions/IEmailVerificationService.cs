namespace GameGuild.Identity.Authentication;

/// <summary>
///     Service for handling email verification operations.
///     Manages verification token generation, validation, and email sending coordination.
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
    ///     Checks if a verification token is still valid (not expired).
    /// </summary>
    /// <param name="token">The verification token</param>
    /// <returns>True if token is valid and not expired</returns>
    Task<bool> IsTokenValidAsync(string token);
}
