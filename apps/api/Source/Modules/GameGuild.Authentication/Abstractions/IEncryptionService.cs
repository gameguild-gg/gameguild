namespace GameGuild.Authentication.Abstractions;

/// <summary>
///     Service for encryption and decryption operations.
///     Used for encrypting sensitive data like MFA secrets, backup codes, and tokens.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    ///     Encrypts sensitive data using application encryption key.
    /// </summary>
    /// <param name="plainText">The plain text to encrypt</param>
    /// <returns>Encrypted data as base64 string</returns>
    string Encrypt(string plainText);

    /// <summary>
    ///     Decrypts previously encrypted data.
    /// </summary>
    /// <param name="cipherText">The encrypted data</param>
    /// <returns>Decrypted plain text</returns>
    string Decrypt(string cipherText);

    /// <summary>
    ///     Generates a cryptographically secure random string.
    /// </summary>
    /// <param name="length">Length of the random string</param>
    /// <returns>Random string</returns>
    string GenerateSecureRandomString(int length);

    /// <summary>
    ///     Generates a secure token for various purposes (refresh tokens, verification codes, etc.).
    /// </summary>
    /// <returns>Cryptographically secure token</returns>
    string GenerateSecureToken();
}
