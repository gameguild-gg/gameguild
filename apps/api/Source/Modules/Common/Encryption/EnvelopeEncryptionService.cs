using System.Security.Cryptography;


namespace GameGuild.Modules.Common.Encryption;

/// <summary>
/// Envelope encryption service using AES-256-GCM.
/// </summary>
public sealed class EnvelopeEncryptionService : IEnvelopeEncryptionService
{
    private readonly ILogger<EnvelopeEncryptionService> _logger;
    private readonly IMasterKeyProvider _masterKeyProvider;

    public EnvelopeEncryptionService(
        ILogger<EnvelopeEncryptionService> logger,
        IMasterKeyProvider masterKeyProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _masterKeyProvider = masterKeyProvider ?? throw new ArgumentNullException(nameof(masterKeyProvider));
    }

    /// <summary>
    /// Encrypts data using envelope encryption.
    /// </summary>
    public async Task<EncryptedData> EncryptAsync(byte[] plaintext, CancellationToken cancellationToken = default)
    {
        if (plaintext == null || plaintext.Length == 0)
            throw new ArgumentException("Plaintext cannot be null or empty", nameof(plaintext));

        try
        {
            // 1. Generate a random Data Encryption Key (DEK)
            var dataKey = new byte[32]; // 256 bits
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(dataKey);
            }

            // 2. Encrypt plaintext with DEK using AES-256-GCM
            byte[] encryptedPlaintext;
            byte[] iv = new byte[12]; // 96 bits for GCM
            byte[] tag = new byte[16]; // 128 bits authentication tag

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(iv);
            }

            using (var aesGcm = new AesGcm(dataKey, 16))
            {
                encryptedPlaintext = new byte[plaintext.Length];
                aesGcm.Encrypt(iv, plaintext, encryptedPlaintext, tag);
            }

            // 3. Get active master key
            var masterKeyId = await _masterKeyProvider.GetActiveKeyIdAsync(cancellationToken);

            // 4. Encrypt DEK with Master Encryption Key (MEK)
            var encryptedDataKey = await _masterKeyProvider.EncryptAsync(masterKeyId, dataKey, cancellationToken);

            // 5. Return envelope encrypted data
            var result = new EncryptedData
            {
                MasterKeyId = masterKeyId,
                EncryptedDataKey = encryptedDataKey,
                EncryptedPlaintext = encryptedPlaintext,
                InitializationVector = iv,
                AuthenticationTag = tag,
                EncryptedAt = DateTime.UtcNow
            };

            _logger.LogDebug(
                "Encrypted {DataSize} bytes using envelope encryption (master key: {KeyId})",
                plaintext.Length, masterKeyId);

            return result;
        }
        finally
        {
            // Clear sensitive data from memory
            Array.Clear(plaintext, 0, plaintext.Length);
        }
    }

    /// <summary>
    /// Decrypts data encrypted with envelope encryption.
    /// </summary>
    public async Task<byte[]> DecryptAsync(EncryptedData encryptedData, CancellationToken cancellationToken = default)
    {
        if (encryptedData == null)
            throw new ArgumentNullException(nameof(encryptedData));

        byte[]? dataKey = null;

        try
        {
            // 1. Decrypt DEK using Master Encryption Key
            dataKey = await _masterKeyProvider.DecryptAsync(
                encryptedData.MasterKeyId,
                encryptedData.EncryptedDataKey,
                cancellationToken);

            // 2. Decrypt plaintext using DEK
            var plaintext = new byte[encryptedData.EncryptedPlaintext.Length];

            using (var aesGcm = new AesGcm(dataKey, 16))
            {
                aesGcm.Decrypt(
                    encryptedData.InitializationVector,
                    encryptedData.EncryptedPlaintext,
                    encryptedData.AuthenticationTag ?? Array.Empty<byte>(),
                    plaintext);
            }

            _logger.LogDebug(
                "Decrypted {DataSize} bytes using envelope encryption (master key: {KeyId})",
                plaintext.Length, encryptedData.MasterKeyId);

            return plaintext;
        }
        finally
        {
            // Clear sensitive data from memory
            if (dataKey != null)
            {
                Array.Clear(dataKey, 0, dataKey.Length);
            }
        }
    }

    /// <summary>
    /// Re-encrypts data with a new master key.
    /// </summary>
    public async Task<EncryptedData> ReEncryptAsync(
        EncryptedData oldData,
        string newKeyId,
        CancellationToken cancellationToken = default)
    {
        if (oldData == null)
            throw new ArgumentNullException(nameof(oldData));
        if (string.IsNullOrWhiteSpace(newKeyId))
            throw new ArgumentException("New key ID cannot be null or empty", nameof(newKeyId));

        byte[]? dataKey = null;

        try
        {
            // 1. Decrypt DEK with old master key
            dataKey = await _masterKeyProvider.DecryptAsync(
                oldData.MasterKeyId,
                oldData.EncryptedDataKey,
                cancellationToken);

            // 2. Encrypt DEK with new master key
            var newEncryptedDataKey = await _masterKeyProvider.EncryptAsync(
                newKeyId,
                dataKey,
                cancellationToken);

            // 3. Return new encrypted data (plaintext remains encrypted with same DEK)
            var result = new EncryptedData
            {
                MasterKeyId = newKeyId,
                EncryptedDataKey = newEncryptedDataKey,
                EncryptedPlaintext = oldData.EncryptedPlaintext,
                InitializationVector = oldData.InitializationVector,
                AuthenticationTag = oldData.AuthenticationTag,
                EncryptedAt = DateTime.UtcNow
            };

            _logger.LogInformation(
                "Re-encrypted data: {OldKeyId} → {NewKeyId}",
                oldData.MasterKeyId, newKeyId);

            return result;
        }
        finally
        {
            // Clear sensitive data from memory
            if (dataKey != null)
            {
                Array.Clear(dataKey, 0, dataKey.Length);
            }
        }
    }
}

/// <summary>
/// Master key provider interface.
/// </summary>
public interface IMasterKeyProvider
{
    Task<string> GetActiveKeyIdAsync(CancellationToken cancellationToken = default);
    Task<byte[]> EncryptAsync(string keyId, byte[] plaintext, CancellationToken cancellationToken = default);
    Task<byte[]> DecryptAsync(string keyId, byte[] ciphertext, CancellationToken cancellationToken = default);
    Task<string> CreateKeyAsync(CancellationToken cancellationToken = default);
}
