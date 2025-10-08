using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameGuild.Modules.Common.Encryption;

/// <summary>
/// Key rotation service for envelope encryption.
/// </summary>
public interface IKeyRotationService
{
    /// <summary>
    /// Rotates the master encryption key.
    /// </summary>
    Task<KeyRotationResult> RotateMasterKeyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-encrypts data with the new master key.
    /// </summary>
    Task<ReEncryptionResult> ReEncryptDataAsync(string keyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current active master key ID.
    /// </summary>
    Task<string> GetActiveKeyIdAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules automatic key rotation.
    /// </summary>
    Task ScheduleRotationAsync(TimeSpan interval, CancellationToken cancellationToken = default);
}

/// <summary>
/// Envelope encryption service for data at rest.
/// </summary>
public interface IEnvelopeEncryptionService
{
    /// <summary>
    /// Encrypts data using envelope encryption (DEK + MEK).
    /// </summary>
    Task<EncryptedData> EncryptAsync(byte[] plaintext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts data encrypted with envelope encryption.
    /// </summary>
    Task<byte[]> DecryptAsync(EncryptedData encryptedData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-encrypts data with a new master key.
    /// </summary>
    Task<EncryptedData> ReEncryptAsync(EncryptedData oldData, string newKeyId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Encrypted data with envelope encryption metadata.
/// </summary>
public sealed class EncryptedData
{
    /// <summary>
    /// ID of the master key used to encrypt the data encryption key.
    /// </summary>
    public required string MasterKeyId { get; init; }

    /// <summary>
    /// Encrypted data encryption key (encrypted with master key).
    /// </summary>
    public required byte[] EncryptedDataKey { get; init; }

    /// <summary>
    /// Encrypted plaintext (encrypted with data encryption key).
    /// </summary>
    public required byte[] EncryptedPlaintext { get; init; }

    /// <summary>
    /// Initialization vector for AES encryption.
    /// </summary>
    public required byte[] InitializationVector { get; init; }

    /// <summary>
    /// Authentication tag for AES-GCM.
    /// </summary>
    public byte[]? AuthenticationTag { get; init; }

    /// <summary>
    /// Timestamp when data was encrypted.
    /// </summary>
    public DateTime EncryptedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Key rotation result.
/// </summary>
public sealed class KeyRotationResult
{
    public required string OldKeyId { get; init; }
    public required string NewKeyId { get; init; }
    public DateTime RotatedAt { get; init; } = DateTime.UtcNow;
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Re-encryption result.
/// </summary>
public sealed class ReEncryptionResult
{
    public int TotalRecords { get; init; }
    public int ReEncryptedRecords { get; init; }
    public int FailedRecords { get; init; }
    public TimeSpan Duration { get; init; }
    public bool Success => FailedRecords == 0;
    public List<string> Errors { get; init; } = new();
}
