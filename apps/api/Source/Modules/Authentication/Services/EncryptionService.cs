using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace GameGuild.Modules.Authentication.Services;

/// <summary>
/// Service for encrypting and decrypting sensitive data like MFA secrets
/// </summary>
public interface IEncryptionService {
    Task<string> EncryptAsync(string plainText);
    Task<string> DecryptAsync(string encryptedText);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}

public class EncryptionService : IEncryptionService {
    private readonly EncryptionOptions _options;
    private readonly ILogger<EncryptionService> _logger;

    public EncryptionService(IOptions<EncryptionOptions> options, ILogger<EncryptionService> logger) {
        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrEmpty(_options.EncryptionKey)) {
            throw new InvalidOperationException("Encryption key is not configured");
        }
    }

    public async Task<string> EncryptAsync(string plainText) {
        try {
            if (string.IsNullOrEmpty(plainText)) {
                return string.Empty;
            }

            using var aes = Aes.Create();
            aes.Key = DeriveKey(_options.EncryptionKey);
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();
            using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
            using var swEncrypt = new StreamWriter(csEncrypt);

            await swEncrypt.WriteAsync(plainText);
            await swEncrypt.FlushAsync();
            await csEncrypt.FlushFinalBlockAsync();

            var encrypted = msEncrypt.ToArray();
            var result = new byte[aes.IV.Length + encrypted.Length];

            Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
            Array.Copy(encrypted, 0, result, aes.IV.Length, encrypted.Length);

            return Convert.ToBase64String(result);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to encrypt data");
            throw;
        }
    }

    public async Task<string> DecryptAsync(string encryptedText) {
        try {
            if (string.IsNullOrEmpty(encryptedText)) {
                return string.Empty;
            }

            var fullCipher = Convert.FromBase64String(encryptedText);

            using var aes = Aes.Create();
            aes.Key = DeriveKey(_options.EncryptionKey);

            var iv = new byte[aes.IV.Length];
            var cipher = new byte[fullCipher.Length - iv.Length];

            Array.Copy(fullCipher, 0, iv, 0, iv.Length);
            Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);

            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(cipher);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);

            return await srDecrypt.ReadToEndAsync();
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to decrypt data");
            throw;
        }
    }

    public string HashPassword(string password) {
        try {
            return BCrypt.Net.BCrypt.HashPassword(password, _options.BcryptWorkFactor);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to hash password");
            throw;
        }
    }

    public bool VerifyPassword(string password, string hash) {
        try {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to verify password");
            return false;
        }
    }

    private byte[] DeriveKey(string password) {
        // Use PBKDF2 to derive a key from the password
        var salt = Encoding.UTF8.GetBytes(_options.Salt);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, _options.KeyDerivationIterations, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(32); // 256-bit key
    }
}

/// <summary>
/// Configuration options for encryption
/// </summary>
public class EncryptionOptions {
    public string EncryptionKey { get; set; } = string.Empty;
    public string Salt { get; set; } = "GameGuild_Salt_2024";
    public int KeyDerivationIterations { get; set; } = 10000;
    public int BcryptWorkFactor { get; set; } = 12;

    public void Validate() {
        if (string.IsNullOrWhiteSpace(EncryptionKey)) {
            throw new InvalidOperationException("Encryption key is required");
        }

        if (EncryptionKey.Length < 32) {
            throw new InvalidOperationException("Encryption key must be at least 32 characters long");
        }

        if (KeyDerivationIterations < 1000) {
            throw new InvalidOperationException("Key derivation iterations must be at least 1000");
        }

        if (BcryptWorkFactor < 4 || BcryptWorkFactor > 31) {
            throw new InvalidOperationException("BCrypt work factor must be between 4 and 31");
        }
    }
}
