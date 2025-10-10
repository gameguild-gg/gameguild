using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameGuild.Modules.Tenants;

/// <summary>
/// Represents a tenant-specific encryption key for data-at-rest encryption.
/// </summary>
[Table("TenantEncryptionKeys")]
[Index(nameof(TenantId), nameof(IsActive), IsUnique = false)]
[Index(nameof(KeyIdentifier), IsUnique = true)]
public class TenantEncryptionKey
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid TenantId { get; set; }

    /// <summary>
    /// Unique identifier for the key (e.g., key version or alias).
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string KeyIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Encrypted data encryption key (DEK) wrapped by master key.
    /// </summary>
    [Required]
    public string EncryptedKey { get; set; } = string.Empty;

    /// <summary>
    /// Algorithm used for encryption (e.g., AES-256-GCM).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Algorithm { get; set; } = "AES-256-GCM";

    /// <summary>
    /// Key derivation function used (e.g., PBKDF2, Argon2).
    /// </summary>
    [MaxLength(50)]
    public string? KeyDerivationFunction { get; set; }

    /// <summary>
    /// Initialization vector used for encryption.
    /// </summary>
    [Required]
    public string InitializationVector { get; set; } = string.Empty;

    /// <summary>
    /// Whether this key is currently active and should be used for new encryptions.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Purpose of the key (e.g., DatabaseEncryption, FileEncryption, BackupEncryption).
    /// </summary>
    [Required]
    public TenantEncryptionKeyPurpose Purpose { get; set; }

    /// <summary>
    /// When the key was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the key was last rotated.
    /// </summary>
    public DateTime? RotatedAt { get; set; }

    /// <summary>
    /// When the key expires and should be rotated.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// When the key was deactivated (soft delete).
    /// </summary>
    public DateTime? DeactivatedAt { get; set; }

    /// <summary>
    /// Metadata about key rotation and usage.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Key usage audit records.
    /// </summary>
    public ICollection<TenantEncryptionKeyUsage> UsageRecords { get; set; } = new List<TenantEncryptionKeyUsage>();

    /// <summary>
    /// Activates the key for use.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        DeactivatedAt = null;
    }

    /// <summary>
    /// Deactivates the key (prevents new encryptions, but allows decryption).
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        DeactivatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the key as rotated.
    /// </summary>
    public void MarkAsRotated()
    {
        RotatedAt = DateTime.UtcNow;
        IsActive = false;
        DeactivatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the key is expired.
    /// </summary>
    public bool IsExpired()
    {
        return ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;
    }

    /// <summary>
    /// Validates the encryption key configuration.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(KeyIdentifier))
            throw new InvalidOperationException("Key identifier is required");

        if (string.IsNullOrWhiteSpace(EncryptedKey))
            throw new InvalidOperationException("Encrypted key is required");

        if (string.IsNullOrWhiteSpace(Algorithm))
            throw new InvalidOperationException("Algorithm is required");

        if (string.IsNullOrWhiteSpace(InitializationVector))
            throw new InvalidOperationException("Initialization vector is required");

        if (ExpiresAt.HasValue && ExpiresAt.Value <= CreatedAt)
            throw new InvalidOperationException("Expiration date must be after creation date");
    }
}

/// <summary>
/// Purpose of the tenant encryption key.
/// </summary>
public enum TenantEncryptionKeyPurpose
{
    DatabaseEncryption = 1,
    FileEncryption = 2,
    BackupEncryption = 3,
    CommunicationEncryption = 4,
    TokenEncryption = 5,
    SecretEncryption = 6
}

/// <summary>
/// Audit record for encryption key usage.
/// </summary>
[Table("TenantEncryptionKeyUsage")]
[Index(nameof(TenantEncryptionKeyId), nameof(OperationType), IsUnique = false)]
public class TenantEncryptionKeyUsage
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid TenantEncryptionKeyId { get; set; }

    /// <summary>
    /// Type of operation performed (Encrypt, Decrypt, Rotate).
    /// </summary>
    [Required]
    public TenantEncryptionKeyOperation OperationType { get; set; }

    /// <summary>
    /// User or service that performed the operation.
    /// </summary>
    [MaxLength(200)]
    public string? PerformedBy { get; set; }

    /// <summary>
    /// Additional context about the operation.
    /// </summary>
    [MaxLength(500)]
    public string? Context { get; set; }

    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if operation failed.
    /// </summary>
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When the operation occurred.
    /// </summary>
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the encryption key.
    /// </summary>
    public TenantEncryptionKey TenantEncryptionKey { get; set; } = null!;
}

/// <summary>
/// Types of operations performed with encryption keys.
/// </summary>
public enum TenantEncryptionKeyOperation
{
    Encrypt = 1,
    Decrypt = 2,
    Rotate = 3,
    Activate = 4,
    Deactivate = 5,
    Export = 6,
    Import = 7
}
