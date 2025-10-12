namespace GameGuild.Modules.Authentication.Services;

/// <summary>
/// Configuration options for encryption
/// </summary>
public class EncryptionOptions
{
    public string EncryptionKey { get; set; } = string.Empty;

    public string Salt { get; set; } = "GameGuild_Salt_2024";

    public int KeyDerivationIterations { get; set; } = 10000;

    public int BcryptWorkFactor { get; set; } = 12;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(EncryptionKey)) { throw new InvalidOperationException("Encryption key is required"); }

        if (EncryptionKey.Length < 32) { throw new InvalidOperationException("Encryption key must be at least 32 characters long"); }

        if (KeyDerivationIterations < 1000) { throw new InvalidOperationException("Key derivation iterations must be at least 1000"); }

        if (BcryptWorkFactor is < 4 or > 31) { throw new InvalidOperationException("BCrypt work factor must be between 4 and 31"); }
    }
}
