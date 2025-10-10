using GameGuild.Modules.Tenants;

namespace GameGuild.Modules.Tenants.Services;

/// <summary>
/// Service for managing tenant-specific encryption keys.
/// </summary>
public interface ITenantEncryptionService
{
    /// <summary>
    /// Generates a new encryption key for a tenant.
    /// </summary>
    Task<TenantEncryptionKey> GenerateKeyAsync(Guid tenantId, TenantEncryptionKeyPurpose purpose, int keySize = 256, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rotates an encryption key (creates new key and deactivates old one).
    /// </summary>
    Task<TenantEncryptionKey> RotateKeyAsync(Guid keyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Encrypts data using the active key for the specified purpose.
    /// </summary>
    Task<(string EncryptedData, string KeyIdentifier)> EncryptAsync(Guid tenantId, string plaintext, TenantEncryptionKeyPurpose purpose, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts data using the specified key.
    /// </summary>
    Task<string> DecryptAsync(string encryptedData, string keyIdentifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the active encryption key for a tenant and purpose.
    /// </summary>
    Task<TenantEncryptionKey?> GetActiveKeyAsync(Guid tenantId, TenantEncryptionKeyPurpose purpose, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates and tests an encryption key.
    /// </summary>
    Task<bool> ValidateKeyAsync(Guid keyId, CancellationToken cancellationToken = default);
}
