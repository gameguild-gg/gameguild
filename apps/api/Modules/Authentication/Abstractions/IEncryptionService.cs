namespace GameGuild.Modules.Authentication;

/// <summary>
/// Service for encrypting and decrypting sensitive data like MFA secrets
/// </summary>
public interface IEncryptionService
{
    Task<string> EncryptAsync(string plainText);

    Task<string> DecryptAsync(string encryptedText);

    string HashPassword(string password);

    bool VerifyPassword(string password, string hash);
}
