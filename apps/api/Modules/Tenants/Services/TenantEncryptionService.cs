using GameGuild.Modules.Tenants.Repositories;
using System.Security.Cryptography;
using System.Text;


namespace GameGuild.Modules.Tenants.Services;

/// <summary>
/// Service implementation for managing tenant-specific encryption keys.
/// </summary>
public class TenantEncryptionService : ITenantEncryptionService
{
    private readonly ITenantEncryptionKeyRepository _repository;
    private readonly ILogger<TenantEncryptionService> _logger;
    private const int DefaultKeySize = 256;

    public TenantEncryptionService(
        ITenantEncryptionKeyRepository repository,
        ILogger<TenantEncryptionService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<TenantEncryptionKey> GenerateKeyAsync(
        Guid tenantId,
        TenantEncryptionKeyPurpose purpose,
        int keySize = DefaultKeySize,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating new encryption key for tenant {TenantId}, purpose {Purpose}", tenantId, purpose);

        // Generate a new data encryption key (DEK)
        var dek = GenerateRandomKey(keySize / 8);
        var iv = GenerateRandomIV();

        // In production, this would be wrapped by a Key Encryption Key (KEK) from a KMS
        var encryptedKey = Convert.ToBase64String(dek);

        var keyIdentifier = $"{tenantId}_{purpose}_{DateTime.UtcNow:yyyyMMddHHmmss}";

        var key = new TenantEncryptionKey
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            KeyIdentifier = keyIdentifier,
            EncryptedKey = encryptedKey,
            Algorithm = "AES-256-GCM",
            InitializationVector = Convert.ToBase64String(iv),
            IsActive = true,
            Purpose = purpose,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddYears(1),
            Metadata = new Dictionary<string, string>
            {
                ["KeySize"] = keySize.ToString(),
                ["GeneratedBy"] = "TenantEncryptionService",
                ["Version"] = "1.0"
            }
        };

        key.Validate();

        // Deactivate any existing active keys for this purpose
        var existingKeys = await _repository.GetActiveKeysAsync(tenantId, purpose, cancellationToken);
        foreach (var existingKey in existingKeys)
        {
            existingKey.Deactivate();
            await _repository.UpdateAsync(existingKey, cancellationToken);
        }

        var createdKey = await _repository.CreateAsync(key, cancellationToken);

        await _repository.RecordUsageAsync(new TenantEncryptionKeyUsage
        {
            Id = Guid.NewGuid(),
            TenantEncryptionKeyId = createdKey.Id,
            OperationType = TenantEncryptionKeyOperation.Activate,
            PerformedBy = "System",
            Context = $"Generated new key for purpose {purpose}",
            Success = true,
            PerformedAt = DateTime.UtcNow
        }, cancellationToken);

        _logger.LogInformation("Successfully generated encryption key {KeyIdentifier}", keyIdentifier);

        return createdKey;
    }

    public async Task<TenantEncryptionKey> RotateKeyAsync(Guid keyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Rotating encryption key {KeyId}", keyId);

        var oldKey = await _repository.GetByIdAsync(keyId, cancellationToken);
        if (oldKey == null)
        {
            throw new InvalidOperationException($"Encryption key {keyId} not found");
        }

        // Generate new key with same purpose
        var newKey = await GenerateKeyAsync(oldKey.TenantId, oldKey.Purpose, cancellationToken: cancellationToken);

        // Mark old key as rotated
        oldKey.MarkAsRotated();
        await _repository.UpdateAsync(oldKey, cancellationToken);

        await _repository.RecordUsageAsync(new TenantEncryptionKeyUsage
        {
            Id = Guid.NewGuid(),
            TenantEncryptionKeyId = oldKey.Id,
            OperationType = TenantEncryptionKeyOperation.Rotate,
            PerformedBy = "System",
            Context = $"Rotated to new key {newKey.KeyIdentifier}",
            Success = true,
            PerformedAt = DateTime.UtcNow
        }, cancellationToken);

        _logger.LogInformation("Successfully rotated key {OldKeyId} to {NewKeyId}", keyId, newKey.Id);

        return newKey;
    }

    public async Task<(string EncryptedData, string KeyIdentifier)> EncryptAsync(
        Guid tenantId,
        string plaintext,
        TenantEncryptionKeyPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        var key = await GetActiveKeyAsync(tenantId, purpose, cancellationToken);
        if (key == null)
        {
            _logger.LogWarning("No active key found for tenant {TenantId}, purpose {Purpose}. Generating new key.", tenantId, purpose);
            key = await GenerateKeyAsync(tenantId, purpose, cancellationToken: cancellationToken);
        }

        try
        {
            var encryptedData = EncryptData(plaintext, key);

            await _repository.RecordUsageAsync(new TenantEncryptionKeyUsage
            {
                Id = Guid.NewGuid(),
                TenantEncryptionKeyId = key.Id,
                OperationType = TenantEncryptionKeyOperation.Encrypt,
                PerformedBy = "System",
                Context = $"Encrypted data for tenant {tenantId}",
                Success = true,
                PerformedAt = DateTime.UtcNow
            }, cancellationToken);

            return (encryptedData, key.KeyIdentifier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt data for tenant {TenantId}", tenantId);

            await _repository.RecordUsageAsync(new TenantEncryptionKeyUsage
            {
                Id = Guid.NewGuid(),
                TenantEncryptionKeyId = key.Id,
                OperationType = TenantEncryptionKeyOperation.Encrypt,
                PerformedBy = "System",
                Context = $"Failed to encrypt data for tenant {tenantId}",
                Success = false,
                ErrorMessage = ex.Message,
                PerformedAt = DateTime.UtcNow
            }, cancellationToken);

            throw;
        }
    }

    public async Task<string> DecryptAsync(string encryptedData, string keyIdentifier, CancellationToken cancellationToken = default)
    {
        var key = await _repository.GetByKeyIdentifierAsync(keyIdentifier, cancellationToken);
        if (key == null)
        {
            throw new InvalidOperationException($"Encryption key {keyIdentifier} not found");
        }

        try
        {
            var plaintext = DecryptData(encryptedData, key);

            await _repository.RecordUsageAsync(new TenantEncryptionKeyUsage
            {
                Id = Guid.NewGuid(),
                TenantEncryptionKeyId = key.Id,
                OperationType = TenantEncryptionKeyOperation.Decrypt,
                PerformedBy = "System",
                Context = "Decrypted data",
                Success = true,
                PerformedAt = DateTime.UtcNow
            }, cancellationToken);

            return plaintext;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt data with key {KeyIdentifier}", keyIdentifier);

            await _repository.RecordUsageAsync(new TenantEncryptionKeyUsage
            {
                Id = Guid.NewGuid(),
                TenantEncryptionKeyId = key.Id,
                OperationType = TenantEncryptionKeyOperation.Decrypt,
                PerformedBy = "System",
                Context = "Failed to decrypt data",
                Success = false,
                ErrorMessage = ex.Message,
                PerformedAt = DateTime.UtcNow
            }, cancellationToken);

            throw;
        }
    }

    public async Task<TenantEncryptionKey?> GetActiveKeyAsync(
        Guid tenantId,
        TenantEncryptionKeyPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        var keys = await _repository.GetActiveKeysAsync(tenantId, purpose, cancellationToken);
        return keys.FirstOrDefault();
    }

    public async Task<bool> ValidateKeyAsync(Guid keyId, CancellationToken cancellationToken = default)
    {
        var key = await _repository.GetByIdAsync(keyId, cancellationToken);
        if (key == null)
        {
            return false;
        }

        try
        {
            key.Validate();

            // Test encryption/decryption
            var testData = "test_validation_data_" + Guid.NewGuid();
            var encrypted = EncryptData(testData, key);
            var decrypted = DecryptData(encrypted, key);

            return testData == decrypted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Key validation failed for key {KeyId}", keyId);
            return false;
        }
    }

    private string EncryptData(string plaintext, TenantEncryptionKey key)
    {
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var keyBytes = Convert.FromBase64String(key.EncryptedKey);
        var iv = Convert.FromBase64String(key.InitializationVector);

        using var aes = Aes.Create();
        aes.Key = keyBytes.Take(32).ToArray(); // Use first 256 bits
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var encryptedBytes = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);

        return Convert.ToBase64String(encryptedBytes);
    }

    private string DecryptData(string encryptedData, TenantEncryptionKey key)
    {
        var encryptedBytes = Convert.FromBase64String(encryptedData);
        var keyBytes = Convert.FromBase64String(key.EncryptedKey);
        var iv = Convert.FromBase64String(key.InitializationVector);

        using var aes = Aes.Create();
        aes.Key = keyBytes.Take(32).ToArray(); // Use first 256 bits
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

        return Encoding.UTF8.GetString(decryptedBytes);
    }

    private byte[] GenerateRandomKey(int length)
    {
        var key = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(key);
        return key;
    }

    private byte[] GenerateRandomIV()
    {
        var iv = new byte[16]; // AES IV is always 128 bits
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(iv);
        return iv;
    }
}
