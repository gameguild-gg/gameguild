
namespace GameGuild.Identity.Authentication;

/// <summary>
///     Service for generating various security tokens (refresh tokens, reset tokens, etc.).
///     Provides consistent token generation with configurable expiration and validation.
/// </summary>
public interface ITokenProvider
{
    /// <summary>
    ///     Generates a refresh token for extending user sessions.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="deviceInfo">Optional device information to bind token to</param>
    /// <returns>Refresh token string</returns>
    Task<string> GenerateRefreshTokenAsync(Guid userId, string? deviceInfo = null);

    /// <summary>
    ///     Generates a password reset token.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="email">The user's email address</param>
    /// <returns>Password reset token</returns>
    Task<string> GeneratePasswordResetTokenAsync(Guid userId, string email);

    /// <summary>
    ///     Generates an email verification token.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="email">The email to verify</param>
    /// <returns>Email verification token</returns>
    Task<string> GenerateEmailVerificationTokenAsync(Guid userId, string email);

    /// <summary>
    ///     Validates any type of token and returns its payload if valid.
    /// </summary>
    /// <param name="token">The token to validate</param>
    /// <param name="tokenType">The expected token type</param>
    /// <returns>Token payload if valid, null otherwise</returns>
    Task<TokenPayload?> ValidateTokenAsync(string token, string tokenType);

    /// <summary>
    ///     Revokes a token making it invalid for future use.
    /// </summary>
    /// <param name="token">The token to revoke</param>
    Task RevokeTokenAsync(string token);
}
