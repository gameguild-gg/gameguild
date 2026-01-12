using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Encryption service using AES-256-GCM for symmetric encryption.
///     Provides encryption/decryption for sensitive data (MFA secrets, tokens, etc.).
/// </summary>
public sealed class EncryptionService(ILogger<EncryptionService> logger) : IEncryptionService
{
    // Encryption key (TODO: Move to secure key vault/configuration)
    // In production, use Azure Key Vault, AWS KMS, or similar
    private const string EncryptionKeyTemp = "GameGuild_Encryption_Key_32_Chars"; // Must be 32 bytes for AES-256

    // AES-GCM parameters
    private const int NonceSize = 12; // 96 bits (recommended for AES-GCM)

    private const int TagSize = 16; // 128 bits (authentication tag)

    /// <summary>
    ///     Encrypts data using AES-256-GCM.
    ///     Returns Base64-encoded string: nonce + ciphertext + tag.
    /// </summary>
    public async Task<string> EncryptAsync(string plaintext, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(plaintext)) { throw new ArgumentException("Plaintext cannot be empty", nameof(plaintext)); }

        try
        {
            logger.LogDebug("Encrypting data with AES-256-GCM");

            // Convert plaintext to bytes
            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);

            // Generate encryption key from constant (TODO: Use secure key management)
            var key = DeriveKey(EncryptionKeyTemp);

            // Generate random nonce (96 bits)
            var nonce = new byte[NonceSize];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(nonce);

            // Prepare buffers
            var ciphertext = new byte[plaintextBytes.Length];
            var tag = new byte[TagSize];

            // Encrypt using AES-GCM
            using var aesGcm = new AesGcm(key, TagSize);
            aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);

            // Combine nonce + ciphertext + tag
            var combined = new byte[NonceSize + ciphertext.Length + TagSize];
            Buffer.BlockCopy(nonce, 0, combined, 0, NonceSize);
            Buffer.BlockCopy(ciphertext, 0, combined, NonceSize, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, combined, NonceSize + ciphertext.Length, TagSize);

            // Return Base64-encoded result
            var encrypted = Convert.ToBase64String(combined);

            logger.LogDebug("Data encrypted successfully, Length: {Length}", encrypted.Length);

            return await Task.FromResult(encrypted);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error encrypting data");

            throw;
        }
    }

    /// <summary>
    ///     Decrypts data encrypted with AES-256-GCM.
    ///     Expects Base64-encoded string: nonce + ciphertext + tag.
    /// </summary>
    public async Task<string> DecryptAsync(string ciphertext, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(ciphertext)) { throw new ArgumentException("Ciphertext cannot be empty", nameof(ciphertext)); }

        try
        {
            logger.LogDebug("Decrypting data with AES-256-GCM");

            // Decode Base64
            var combined = Convert.FromBase64String(ciphertext);

            if (combined.Length < NonceSize + TagSize) { throw new CryptographicException("Invalid ciphertext format"); }

            // Extract nonce, ciphertext, and tag
            var nonce = new byte[NonceSize];
            var ciphertextBytes = new byte[combined.Length - NonceSize - TagSize];
            var tag = new byte[TagSize];

            Buffer.BlockCopy(combined, 0, nonce, 0, NonceSize);
            Buffer.BlockCopy(combined, NonceSize, ciphertextBytes, 0, ciphertextBytes.Length);
            Buffer.BlockCopy(combined, NonceSize + ciphertextBytes.Length, tag, 0, TagSize);

            // Generate encryption key
            var key = DeriveKey(EncryptionKeyTemp);

            // Decrypt using AES-GCM
            var plaintext = new byte[ciphertextBytes.Length];

            using var aesGcm = new AesGcm(key, TagSize);
            aesGcm.Decrypt(nonce, ciphertextBytes, tag, plaintext);

            // Convert to string
            var decrypted = Encoding.UTF8.GetString(plaintext);

            logger.LogDebug("Data decrypted successfully");

            return await Task.FromResult(decrypted);
        }
        catch (CryptographicException ex)
        {
            logger.LogError(ex, "Decryption failed - data may be corrupted or tampered");

            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error decrypting data");

            throw;
        }
    }

    /// <summary>
    ///     Generates a cryptographically secure random token.
    /// </summary>
    public async Task<string> GenerateSecureTokenAsync(int length = 32, CancellationToken cancellationToken = default)
    {
        if (length <= 0) { throw new ArgumentException("Token length must be positive", nameof(length)); }

        try
        {
            logger.LogDebug("Generating secure token, Length: {Length}", length);

            var tokenBytes = new byte[length];

            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(tokenBytes);

            // Return Base64-encoded token (URL-safe)
            var token = Convert.ToBase64String(tokenBytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');

            logger.LogDebug("Secure token generated, Length: {Length}", token.Length);

            return await Task.FromResult(token);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating secure token");

            throw;
        }
    }

    /// <summary>
    ///     Validates a secure token format.
    /// </summary>
    public async Task<bool> ValidateSecureTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) { return false; }

        try
        {
            // Check if token is valid Base64URL format
            var base64 = token.Replace("-", "+").Replace("_", "/");

            // Add padding if needed
            switch (base64.Length % 4)
            {
                case 2 : base64 += "=="; break;
                case 3 : base64 += "="; break;
            }

            var tokenBytes = Convert.FromBase64String(base64);

            // Token should be at least 16 bytes (128 bits)
            var isValid = tokenBytes.Length >= 16;

            logger.LogDebug("Token validation result: {IsValid}", isValid);

            return await Task.FromResult(isValid);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Token validation failed: Invalid format");

            return false;
        }
    }

    #region Private Helper Methods

    /// <summary>
    ///     Derives a 32-byte key from the encryption key constant.
    ///     In production, use proper key derivation (PBKDF2, HKDF, etc.).
    /// </summary>
    private byte[ ] DeriveKey(string keyString)
    {
        // Simple key derivation (TODO: Use PBKDF2 or HKDF in production)
        using var sha256 = SHA256.Create();
        var keyBytes = Encoding.UTF8.GetBytes(keyString);
        var derivedKey = sha256.ComputeHash(keyBytes);

        // AES-256 requires 32 bytes
        if (derivedKey.Length != 32) { throw new CryptographicException("Invalid key length for AES-256"); }

        return derivedKey;
    }

    #endregion

    #region Interface Implementation (Synchronous Wrappers)

    /// <summary>
    ///     Synchronous wrapper for EncryptAsync.
    /// </summary>
    public string Encrypt(string plainText) { return EncryptAsync(plainText, CancellationToken.None).GetAwaiter().GetResult(); }

    /// <summary>
    ///     Synchronous wrapper for DecryptAsync.
    /// </summary>
    public string Decrypt(string cipherText) { return DecryptAsync(cipherText, CancellationToken.None).GetAwaiter().GetResult(); }

    /// <summary>
    ///     Generates a cryptographically secure random string.
    /// </summary>
    public string GenerateSecureRandomString(int length)
    {
        if (length <= 0) { throw new ArgumentException("Length must be positive", nameof(length)); }

        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var randomBytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        var result = new char[length];

        for (var i = 0; i < length; i++) { result[i] = chars[randomBytes[i] % chars.Length]; }

        return new string(result);
    }

    /// <summary>
    ///     Synchronous wrapper for GenerateSecureTokenAsync.
    /// </summary>
    public string GenerateSecureToken() { return GenerateSecureTokenAsync(32, CancellationToken.None).GetAwaiter().GetResult(); }

    #endregion
}
