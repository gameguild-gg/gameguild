using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Outcome of resolving the active credential for a (provider, secret name) slot: the
///     decrypted plaintext plus identity metadata. Instances carry a live credential —
///     callers must never log <see cref="Plaintext" />.
/// </summary>
/// <param name="ProviderKey">Gateway provider identifier (normalized lowercase).</param>
/// <param name="SecretName">Credential slot on that gateway (normalized lowercase).</param>
/// <param name="Plaintext">Decrypted credential value; treat as a secret.</param>
/// <param name="Fingerprint">SHA-256 fingerprint (hex prefix) of the plaintext.</param>
/// <param name="ActivatedAtUtc">UTC instant the credential became active.</param>
public sealed record GatewayCredentialSecret(
    string ProviderKey,
    string SecretName,
    string Plaintext,
    string Fingerprint,
    DateTime ActivatedAtUtc);

/// <summary>
///     Result of provisioning a gateway credential: identity metadata of the new active
///     row. Deliberately carries no plaintext — the value exists only encrypted at rest.
/// </summary>
/// <param name="ProviderKey">Gateway provider identifier (normalized lowercase).</param>
/// <param name="SecretName">Credential slot that was provisioned (normalized lowercase).</param>
/// <param name="Fingerprint">SHA-256 fingerprint (hex prefix) of the provisioned plaintext.</param>
/// <param name="ActivatedAtUtc">UTC instant the new credential became active.</param>
/// <param name="DeactivatedPrevious">Whether a previously active credential was deactivated by this provisioning.</param>
public sealed record GatewayCredentialProvisionResult(
    string ProviderKey,
    string SecretName,
    string Fingerprint,
    DateTime ActivatedAtUtc,
    bool DeactivatedPrevious);

/// <summary>
///     AES-256-encrypted-at-rest store for payment-gateway API credentials (issue #413).
///     Credentials are provisioned as ASP.NET DataProtection payloads (purpose
///     <c>Payments.GatewayCredentials</c>; the framework applies AES-256-CBC under the
///     application key ring) and never persist or log plaintext. Reading returns the active
///     decrypted secret; a row that cannot be decrypted (for example after a key-ring loss)
///     is skipped as absent — fail closed to the caller's fallback, never a guess.
/// </summary>
public interface IGatewayCredentialStore
{
    /// <summary>
    ///     Resolves the active credential for a (provider, secret name) slot, or null when
    ///     no active row exists or the stored payload cannot be decrypted.
    /// </summary>
    Task<GatewayCredentialSecret?> GetActiveAsync(
        string providerKey,
        string secretName,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Provisions a new credential for the slot: encrypts the plaintext, deactivates the
    ///     previously active row (if any) and inserts the new active row atomically.
    /// </summary>
    Task<GatewayCredentialProvisionResult> ProvisionAsync(
        string providerKey,
        string secretName,
        string plaintextSecret,
        Guid? provisionedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deactivates the active credential of the slot. Returns true when an active row
    ///     was deactivated; false when the slot had no active credential.
    /// </summary>
    Task<bool> DeactivateAsync(
        string providerKey,
        string secretName,
        Guid? deactivatedByUserId,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     Default <see cref="IGatewayCredentialStore" />. Registered as a singleton: database
///     access goes through a scope per call, and DataProtection decryption is stateless.
/// </summary>
public sealed class GatewayCredentialStore(
    IServiceScopeFactory scopeFactory,
    IDataProtectionProvider protectionProvider,
    ILogger<GatewayCredentialStore> logger) : IGatewayCredentialStore
{
    /// <summary>DataProtection purpose string isolating gateway credentials from other protected payloads.</summary>
    public const string ProtectionPurpose = "Payments.GatewayCredentials";

    private const int FingerprintLength = 16;

    private readonly IDataProtector _protector = protectionProvider.CreateProtector(ProtectionPurpose);

    /// <inheritdoc />
    public async Task<GatewayCredentialSecret?> GetActiveAsync(
        string providerKey,
        string secretName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(secretName);

        var normalizedProvider = providerKey.ToLowerInvariant();
        var normalizedSecret = secretName.ToLowerInvariant();

        GatewayCredential? credential;
        using (var scope = scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            credential = await context.Set<GatewayCredential>()
                .AsNoTracking()
                .Where(candidate =>
                    candidate.ProviderKey == normalizedProvider &&
                    candidate.SecretName == normalizedSecret &&
                    candidate.DeactivatedAtUtc == null)
                .OrderByDescending(candidate => candidate.ActivatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        if (credential is null)
        {
            return null;
        }

        string? plaintext;
        try
        {
            plaintext = _protector.Unprotect(credential.EncryptedValue);
        }
        catch (Exception exception)
        {
            // An undecryptable payload (key-ring loss, tampering) must surface as "no
            // credential", so callers fail closed to their explicit fallback.
            logger.LogError(
                exception,
                "Failed to decrypt the stored {ProviderKey}/{SecretName} gateway credential {CredentialId}; treating it as absent",
                normalizedProvider,
                normalizedSecret,
                credential.Id);
            return null;
        }

        if (string.IsNullOrEmpty(plaintext))
        {
            logger.LogError(
                "The stored {ProviderKey}/{SecretName} gateway credential {CredentialId} decrypted to an empty value; treating it as absent",
                normalizedProvider,
                normalizedSecret,
                credential.Id);
            return null;
        }

        return new GatewayCredentialSecret(
            normalizedProvider,
            normalizedSecret,
            plaintext,
            credential.Fingerprint,
            credential.ActivatedAtUtc);
    }

    /// <inheritdoc />
    public async Task<GatewayCredentialProvisionResult> ProvisionAsync(
        string providerKey,
        string secretName,
        string plaintextSecret,
        Guid? provisionedByUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(secretName);
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintextSecret);

        var normalizedProvider = providerKey.ToLowerInvariant();
        var normalizedSecret = secretName.ToLowerInvariant();
        var nowUtc = SystemClock.UtcNow;

        // Encrypt first: even an aborted provisioning never leaves plaintext anywhere but
        // memory, and the fingerprint lets operators verify what was stored.
        var encryptedValue = _protector.Protect(plaintextSecret);
        var fingerprint = ComputeFingerprint(plaintextSecret);
        var credential = new GatewayCredential(
            normalizedProvider,
            normalizedSecret,
            encryptedValue,
            fingerprint,
            nowUtc,
            provisionedByUserId);

        var deactivatedPrevious = false;
        using (var scope = scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            var activeRows = await context.Set<GatewayCredential>()
                .Where(candidate =>
                    candidate.ProviderKey == normalizedProvider &&
                    candidate.SecretName == normalizedSecret &&
                    candidate.DeactivatedAtUtc == null)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var activeRow in activeRows)
            {
                activeRow.Deactivate(nowUtc);
                deactivatedPrevious = true;
            }

            context.Set<GatewayCredential>().Add(credential);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation(
            "Provisioned gateway credential {ProviderKey}/{SecretName} (fingerprint {Fingerprint}, previous deactivated: {DeactivatedPrevious})",
            normalizedProvider,
            normalizedSecret,
            fingerprint,
            deactivatedPrevious);

        return new GatewayCredentialProvisionResult(
            normalizedProvider,
            normalizedSecret,
            fingerprint,
            nowUtc,
            deactivatedPrevious);
    }

    /// <inheritdoc />
    public async Task<bool> DeactivateAsync(
        string providerKey,
        string secretName,
        Guid? deactivatedByUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(secretName);

        var normalizedProvider = providerKey.ToLowerInvariant();
        var normalizedSecret = secretName.ToLowerInvariant();
        var nowUtc = SystemClock.UtcNow;
        var deactivatedAny = false;

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var activeRows = await context.Set<GatewayCredential>()
            .Where(candidate =>
                candidate.ProviderKey == normalizedProvider &&
                candidate.SecretName == normalizedSecret &&
                candidate.DeactivatedAtUtc == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var activeRow in activeRows)
        {
            activeRow.Deactivate(nowUtc);
            deactivatedAny = true;
        }

        if (deactivatedAny)
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation(
                "Deactivated the gateway credential {ProviderKey}/{SecretName}",
                normalizedProvider,
                normalizedSecret);
        }

        return deactivatedAny;
    }

    /// <summary>
    ///     SHA-256 fingerprint of the plaintext (hex, first 16 characters): strong enough to
    ///     compare two provisioned secrets for equality without storing or decrypting them.
    /// </summary>
    internal static string ComputeFingerprint(string plaintext)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(plaintext));
        return Convert.ToHexString(hash)[..FingerprintLength].ToLowerInvariant();
    }
}
