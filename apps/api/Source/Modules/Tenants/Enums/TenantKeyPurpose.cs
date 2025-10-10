namespace GameGuild.Modules.Tenants.Enums;

/// <summary>
/// Defines the purpose/usage of a tenant encryption key
/// </summary>
public enum TenantKeyPurpose
{
    /// <summary>General data encryption</summary>
    DataEncryption = 0,

    /// <summary>Token signing and verification</summary>
    TokenSigning = 1,

    /// <summary>API key encryption</summary>
    ApiKeyEncryption = 2,

    /// <summary>File encryption</summary>
    FileEncryption = 3,

    /// <summary>Database encryption</summary>
    DatabaseEncryption = 4,

    /// <summary>Backup encryption</summary>
    BackupEncryption = 5
}