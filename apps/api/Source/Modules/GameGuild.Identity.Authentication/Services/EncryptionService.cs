using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Encryption service using AES-256-GCM for symmetric encryption.
///     Provides encryption/decryption for sensitive data (MFA secrets, tokens, etc.).
/// </summary>
public sealed class EncryptionService(ILogger<EncryptionService> logger, IConfiguration configuration) : IEncryptionService
{
    /// <summary>
    ///     Fallback key used only when no key is configured. Logs a warning on first use.
    ///     In production, set Encryption:Key in configuration or use a key vault.
    /// </summary>
    private const string FallbackKey = "GameGuild_Encryption_Key_32_Chars";

    private string EncryptionKey
    {
        get
        {
            var configuredKey = configuration["Encryption:Key"];
            if (!string.IsNullOrEmpty(configuredKey))
                return configuredKey;

            logger.LogWarning("Encryption:Key not configured — using insecure fallback key. Set Encryption:Key in configuration for production");
            return FallbackKey;
        }
    }

    // AES-GCM parameters
    private const int NonceSize = 12; // 96 bits (recommended for AES-GCM)

    private const int TagSize = 16; // 128 bits (authentication tag)

    /// <summary>
    ///     Encrypts data using AES-256-GCM.
    ///     Returns Base64-encoded string: nonce + ciphertext + tag.
    /// </summary>
    public Task<string> EncryptAsync(string plaintext, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Encrypt(plaintext));
    }

    /// <summary>
    ///     Decrypts data encrypted with AES-256-GCM.
    ///     Expects Base64-encoded string: nonce + ciphertext + tag.
    /// </summary>
    public Task<string> DecryptAsync(string ciphertext, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Decrypt(ciphertext));
    }

    /// <summary>
    ///     Generates a cryptographically secure random token.
    /// </summary>
    public Task<string> GenerateSecureTokenAsync(int length = 32, CancellationToken cancellationToken = default)
    {
        if (length <= 0) { throw new ArgumentException("Token length must be positive", nameof(length)); }

        logger.LogDebug("Generating secure token, Length: {Length}", length);

        var tokenBytes = new byte[length];

        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(tokenBytes);

        // Return Base64-encoded token (URL-safe)
        var token = Convert.ToBase64String(tokenBytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');

        logger.LogDebug("Secure token generated, Length: {Length}", token.Length);

        return Task.FromResult(token);
    }

    /// <summary>
    ///     Validates a secure token format.
    /// </summary>
    public Task<bool> ValidateSecureTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) { return Task.FromResult(false); }

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

            return Task.FromResult(isValid);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Token validation failed: Invalid format");

            return Task.FromResult(false);
        }
    }

    #region Private Helper Methods

    /// <summary>
    ///     Derives a 32-byte key from the encryption key using HKDF (RFC 5869).
    /// </summary>
    private byte[ ] DeriveKey(string keyString)
    {
        var keyBytes = Encoding.UTF8.GetBytes(keyString);
        var info = Encoding.UTF8.GetBytes("GameGuild.Encryption.AES256GCM");

        // HKDF: extract-then-expand with SHA-256 produces exactly 32 bytes for AES-256
        var derivedKey = HKDF.DeriveKey(HashAlgorithmName.SHA256, keyBytes, 32, salt: null, info);

        return derivedKey;
    }

    #endregion

    #region Interface Implementation (Synchronous Methods)

    /// <summary>
    ///     Encrypts data using AES-256-GCM.
    ///     Returns Base64-encoded string: nonce + ciphertext + tag.
    /// </summary>
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) { throw new ArgumentException("Plaintext cannot be empty", nameof(plainText)); }

        logger.LogDebug("Encrypting data with AES-256-GCM");

        var plaintextBytes = Encoding.UTF8.GetBytes(plainText);
        var key = DeriveKey(EncryptionKey);

        var nonce = new byte[NonceSize];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(nonce);

        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];

        using var aesGcm = new AesGcm(key, TagSize);
        aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var combined = new byte[NonceSize + ciphertext.Length + TagSize];
        Buffer.BlockCopy(nonce, 0, combined, 0, NonceSize);
        Buffer.BlockCopy(ciphertext, 0, combined, NonceSize, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, combined, NonceSize + ciphertext.Length, TagSize);

        var encrypted = Convert.ToBase64String(combined);
        logger.LogDebug("Data encrypted successfully, Length: {Length}", encrypted.Length);

        return encrypted;
    }

    /// <summary>
    ///     Decrypts data encrypted with AES-256-GCM.
    ///     Expects Base64-encoded string: nonce + ciphertext + tag.
    /// </summary>
    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) { throw new ArgumentException("Ciphertext cannot be empty", nameof(cipherText)); }

        try
        {
            logger.LogDebug("Decrypting data with AES-256-GCM");

            var combined = Convert.FromBase64String(cipherText);
            if (combined.Length < NonceSize + TagSize) { throw new CryptographicException("Invalid ciphertext format"); }

            var nonce = new byte[NonceSize];
            var ciphertextBytes = new byte[combined.Length - NonceSize - TagSize];
            var tag = new byte[TagSize];

            Buffer.BlockCopy(combined, 0, nonce, 0, NonceSize);
            Buffer.BlockCopy(combined, NonceSize, ciphertextBytes, 0, ciphertextBytes.Length);
            Buffer.BlockCopy(combined, NonceSize + ciphertextBytes.Length, tag, 0, TagSize);

            var key = DeriveKey(EncryptionKey);
            var plaintext = new byte[ciphertextBytes.Length];

            using var aesGcm = new AesGcm(key, TagSize);
            aesGcm.Decrypt(nonce, ciphertextBytes, tag, plaintext);

            var decrypted = Encoding.UTF8.GetString(plaintext);
            logger.LogDebug("Data decrypted successfully");

            return decrypted;
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
    ///     Generates a cryptographically secure token.
    /// </summary>
    public string GenerateSecureToken()
    {
        return GenerateSecureTokenAsync(32, CancellationToken.None).GetAwaiter().GetResult();
    }

    #endregion
}
