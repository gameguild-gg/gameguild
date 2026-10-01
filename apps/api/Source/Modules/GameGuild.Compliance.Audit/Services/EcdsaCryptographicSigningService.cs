using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace GameGuild.Compliance.Audit;

/// <summary>
/// Computes SHA-256 audit hashes and ECDSA signatures using keys provisioned outside the database.
/// </summary>
public sealed class EcdsaCryptographicSigningService : ICryptographicSigningService
{
    private readonly IReadOnlyDictionary<string, AuditSigningKeyOptions> _keys;
    private string? _activeKeyId;

    public EcdsaCryptographicSigningService(IOptions<AuditSigningOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _keys = new Dictionary<string, AuditSigningKeyOptions>(
            options.Value.Keys,
            StringComparer.Ordinal);
        _activeKeyId = options.Value.ActiveKeyId;
    }

    public string GetActiveKeyId()
    {
        return Volatile.Read(ref _activeKeyId)
               ?? throw new InvalidOperationException("No active audit signing key is configured.");
    }

    public string ComputeContentHash(string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return ComputeHash(content);
    }

    public string ComputeChainHash(string contentHash, string previousHash, long sequenceNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentHash);
        ArgumentNullException.ThrowIfNull(previousHash);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sequenceNumber);

        return ComputeHash($"{sequenceNumber}\n{previousHash}\n{contentHash}");
    }

    public string SignData(string data, string keyId)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);

        using var key = ImportPrivateKey(keyId);
        var signature = key.SignData(
            Encoding.UTF8.GetBytes(data),
            HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

        return Convert.ToBase64String(signature);
    }

    public bool VerifySignature(string data, string signature, string keyId)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(signature);

        if (!_keys.TryGetValue(keyId, out var keyOptions))
        {
            return false;
        }

        try
        {
            using var key = ImportPublicKey(keyOptions);
            return key.VerifyData(
                Encoding.UTF8.GetBytes(data),
                Convert.FromBase64String(signature),
                HashAlgorithmName.SHA256,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        }
        catch (FormatException)
        {
            return false;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    public Task<Result<string>> GetPublicKeyAsync(string keyId)
        => GetPublicKeyAsync(keyId, CancellationToken.None);

    public Task<Result<string>> GetPublicKeyAsync(string keyId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_keys.TryGetValue(keyId, out var keyOptions))
        {
            return Task.FromResult(Result.Failure<string>(
                Error.NotFound("AuditSigning.KeyNotFound", $"Audit signing key '{keyId}' is not configured.")));
        }

        try
        {
            using var key = ImportPublicKey(keyOptions);
            return Task.FromResult(Result.Success(key.ExportSubjectPublicKeyInfoPem()));
        }
        catch (CryptographicException)
        {
            return Task.FromResult(Result.Failure<string>(
                Error.Failure("AuditSigning.InvalidPublicKey", $"Audit signing key '{keyId}' has invalid key material.")));
        }
        catch (InvalidOperationException)
        {
            return Task.FromResult(Result.Failure<string>(
                Error.Failure("AuditSigning.PublicKeyUnavailable", $"Audit signing key '{keyId}' has no public key configured.")));
        }
    }

    public Task<Result> RotateSigningKeyAsync(string newKeyId)
        => RotateSigningKeyAsync(newKeyId, CancellationToken.None);

    public Task<Result> RotateSigningKeyAsync(string newKeyId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(newKeyId) || !_keys.ContainsKey(newKeyId))
        {
            return Task.FromResult(Result.Failure(
                Error.NotFound("AuditSigning.KeyNotFound", "The requested audit signing key is not configured.")));
        }

        try
        {
            using var _ = ImportPrivateKey(newKeyId);
            Interlocked.Exchange(ref _activeKeyId, newKeyId);
            return Task.FromResult(Result.Success());
        }
        catch (CryptographicException)
        {
            return Task.FromResult(Result.Failure(
                Error.Failure("AuditSigning.PrivateKeyUnavailable", "The requested audit signing key has no valid private key.")));
        }
        catch (InvalidOperationException)
        {
            return Task.FromResult(Result.Failure(
                Error.Failure("AuditSigning.PrivateKeyUnavailable", "The requested audit signing key has no private key configured.")));
        }
    }

    private ECDsa ImportPrivateKey(string keyId)
    {
        if (!_keys.TryGetValue(keyId, out var keyOptions) || string.IsNullOrWhiteSpace(keyOptions.PrivateKeyPem))
        {
            throw new InvalidOperationException($"No private audit signing key is configured for '{keyId}'.");
        }

        var key = ECDsa.Create();

        try
        {
            key.ImportFromPem(keyOptions.PrivateKeyPem);
            return key;
        }
        catch
        {
            key.Dispose();
            throw;
        }
    }

    private static ECDsa ImportPublicKey(AuditSigningKeyOptions keyOptions)
    {
        var key = ECDsa.Create();

        try
        {
            if (!string.IsNullOrWhiteSpace(keyOptions.PublicKeyPem))
            {
                key.ImportFromPem(keyOptions.PublicKeyPem);
            }
            else if (!string.IsNullOrWhiteSpace(keyOptions.PrivateKeyPem))
            {
                key.ImportFromPem(keyOptions.PrivateKeyPem);
            }
            else
            {
                throw new InvalidOperationException("No public audit signing key is configured.");
            }

            return key;
        }
        catch
        {
            key.Dispose();
            throw;
        }
    }

    private static string ComputeHash(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
