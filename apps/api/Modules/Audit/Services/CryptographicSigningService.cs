using System.Security.Cryptography;
using GameGuild.Modules.Audit.Entities;
using System.Text;
using System.Text.Json;


namespace GameGuild.Modules.Audit.Services;

/// <summary>
/// Service for cryptographic operations on audit logs including hashing and digital signatures
/// </summary>
public class CryptographicSigningService : ICryptographicSigningService
{
    private readonly ILogger<CryptographicSigningService> _logger;
    private readonly Dictionary<string, RSA> _signingKeys;

    public CryptographicSigningService(ILogger<CryptographicSigningService> logger)
    {
        _logger = logger;
        _signingKeys = new Dictionary<string, RSA>();

        // Initialize default signing key
        InitializeDefaultKey();
    }

    // Interface method - accepts string content
    public string ComputeContentHash(string content)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToBase64String(hashBytes);
    }

    // Internal method - accepts audit log
    private string ComputeContentHash(TamperEvidentAuditLog auditLog)
    {
        // Create a canonical representation of the audit log for hashing
        var content = new
        {
            auditLog.TenantId,
            auditLog.UserId,
            auditLog.Action,
            auditLog.EntityType,
            auditLog.EntityId,
            auditLog.BeforeSnapshot,
            auditLog.AfterSnapshot,
            auditLog.Changes,
            auditLog.RiskLevel,
            auditLog.IpAddress,
            auditLog.UserAgent,
            auditLog.Country,
            auditLog.Region,
            auditLog.City,
            auditLog.Timestamp
        };

        var json = JsonSerializer.Serialize(content, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = null
        });

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToBase64String(hashBytes);
    }

    public string ComputeChainHash(string contentHash, string previousHash, long sequenceNumber)
    {
        var chainData = $"{contentHash}|{previousHash}|{sequenceNumber}";

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(chainData));
        return Convert.ToBase64String(hashBytes);
    }

    // Interface method - accepts string data and keyId
    public string SignData(string data, string keyId)
    {
        if (!_signingKeys.TryGetValue(keyId, out var rsa))
        {
            throw new InvalidOperationException($"Signing key {keyId} not found");
        }

        var dataBytes = Encoding.UTF8.GetBytes(data);
        var signature = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        return Convert.ToBase64String(signature);
    }

    // Internal async method for backward compatibility
    private async Task<string> SignData(string data, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // For async consistency

        var keyId = "default-key-2024";
        if (!_signingKeys.TryGetValue(keyId, out var rsa))
        {
            throw new InvalidOperationException($"Signing key {keyId} not found");
        }

        var dataBytes = Encoding.UTF8.GetBytes(data);
        var signature = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        return Convert.ToBase64String(signature);
    }

    // Interface method - accepts string data, signature, and keyId
    public bool VerifySignature(string data, string signature, string keyId)
    {
        try
        {
            if (!_signingKeys.TryGetValue(keyId, out var rsa))
            {
                _logger.LogWarning("Signing key {KeyId} not found for verification", keyId);
                return false;
            }

            var dataBytes = Encoding.UTF8.GetBytes(data);
            var signatureBytes = Convert.FromBase64String(signature);

            return rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying signature with key {KeyId}", keyId);
            return false;
        }
    }

    // Internal async method for backward compatibility
    private async Task<bool> VerifySignature(
        string data,
        string signature,
        string keyId,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // For async consistency

        try
        {
            if (!_signingKeys.TryGetValue(keyId, out var rsa))
            {
                _logger.LogWarning("Signing key {KeyId} not found for verification", keyId);
                return false;
            }

            var dataBytes = Encoding.UTF8.GetBytes(data);
            var signatureBytes = Convert.FromBase64String(signature);

            return rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying signature with key {KeyId}", keyId);
            return false;
        }
    }

    public async Task<Result<string>> GetPublicKeyAsync(string keyId, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // For async consistency

        if (!_signingKeys.TryGetValue(keyId, out var rsa))
        {
            return Result<string>.Failure("Signing key not found");
        }

        var publicKey = rsa.ExportSubjectPublicKeyInfo();
        return Result<string>.Success(Convert.ToBase64String(publicKey));
    }

    public async Task<Result> RotateSigningKeyAsync(string newKeyId, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // For async consistency

        var newRsa = RSA.Create(2048);
        _signingKeys[newKeyId] = newRsa;

        _logger.LogInformation("Rotated signing key to {NewKeyId}", newKeyId);

        return Result.Success();
    }

    private void InitializeDefaultKey()
    {
        // In production, load from secure key storage (Azure Key Vault, AWS KMS, etc.)
        var rsa = RSA.Create(2048);
        _signingKeys["default-key-2024"] = rsa;

        _logger.LogInformation("Initialized default signing key");
    }
}
