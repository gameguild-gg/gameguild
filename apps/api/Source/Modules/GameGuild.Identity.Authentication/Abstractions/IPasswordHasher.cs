
namespace GameGuild.Identity.Authentication;

/// <summary>
///     Service for secure password hashing and verification.
///     Supports multiple hashing algorithms (BCrypt, Argon2, etc.) with automatic algorithm upgrades.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    ///     Hashes a password using the current default algorithm (e.g., Argon2id).
    /// </summary>
    /// <param name="password">The plain text password</param>
    /// <returns>Hashed password with algorithm identifier</returns>
    string HashPassword(string password);

    /// <summary>
    ///     Verifies a password against a stored hash.
    ///     Automatically handles multiple hash formats for backward compatibility.
    /// </summary>
    /// <param name="hashedPassword">The stored password hash</param>
    /// <param name="providedPassword">The password to verify</param>
    /// <returns>True if password matches</returns>
    bool VerifyPassword(string hashedPassword, string providedPassword);

    /// <summary>
    ///     Checks if a password hash needs to be upgraded to a newer algorithm.
    /// </summary>
    /// <param name="hashedPassword">The stored password hash</param>
    /// <returns>True if hash should be upgraded</returns>
    bool NeedsUpgrade(string hashedPassword);

    /// <summary>
    ///     Validates password strength against security requirements.
    /// </summary>
    /// <param name="password">The password to validate</param>
    /// <returns>Validation result with specific requirement failures</returns>
    PasswordStrengthResult ValidatePasswordStrength(string password);
}
