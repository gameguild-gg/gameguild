namespace GameGuild.Features;

/// <summary>
///     Service for encrypting and decrypting sensitive feature flag data
/// </summary>
public interface IFeatureFlagEncryptionService
{
    /// <summary>
    ///     Encrypts sensitive flag value data
    /// </summary>
    Task<string> EncryptAsync(string plainText);

    /// <summary>
    ///     Decrypts sensitive flag value data
    /// </summary>
    Task<string> DecryptAsync(string cipherText);

    /// <summary>
    ///     Checks if a value is encrypted
    /// </summary>
    bool IsEncrypted(string value);

    /// <summary>
    ///     Generates a new encryption key
    /// </summary>
    string GenerateEncryptionKey();
}
