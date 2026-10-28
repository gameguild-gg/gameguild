using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     One provisioned payment-gateway credential secret (issue #413): the encrypted-at-rest
///     store for gateway API credentials (Stripe secret keys, PayPal client secrets,
///     Authorize.Net transaction keys, ...). The row never contains plaintext — the value is
///     an ASP.NET DataProtection payload (AES-256-CBC under the application key ring, purpose
///     <c>Payments.GatewayCredentials</c>) — plus a SHA-256 fingerprint so operators can
///     compare the provisioned secret against a known value without decrypting it. At most
///     one active row exists per (provider, secret name); re-provisioning deactivates the
///     previous row. Platform-global by design: gateway credentials are deployment-level
///     configuration, not tenant data.
/// </summary>
[Table("gateway_credentials")]
[Index(nameof(ProviderKey), nameof(SecretName), nameof(DeactivatedAtUtc), Name = "ix_gateway_credentials_lookup")]
public class GatewayCredential : EntityBase
{
    /// <summary>Parameterless constructor for EF Core.</summary>
    private GatewayCredential()
    {
    }

    /// <summary>Creates an active credential row.</summary>
    /// <param name="providerKey">Gateway provider identifier (stripe, paypal, authorizenet, ...).</param>
    /// <param name="secretName">Credential slot on that gateway (api_key, client_secret, transaction_key, ...).</param>
    /// <param name="encryptedValue">DataProtection-encrypted secret payload; never plaintext.</param>
    /// <param name="fingerprint">SHA-256 fingerprint (hex prefix) of the plaintext secret.</param>
    /// <param name="activatedAtUtc">UTC instant the credential becomes the active one.</param>
    /// <param name="provisionedByUserId">Acting administrator; null for system actions.</param>
    public GatewayCredential(
        string providerKey,
        string secretName,
        string encryptedValue,
        string fingerprint,
        DateTime activatedAtUtc,
        Guid? provisionedByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(secretName);
        ArgumentException.ThrowIfNullOrWhiteSpace(encryptedValue);
        ArgumentException.ThrowIfNullOrWhiteSpace(fingerprint);

        ProviderKey = providerKey.ToLowerInvariant();
        SecretName = secretName.ToLowerInvariant();
        EncryptedValue = encryptedValue;
        Fingerprint = fingerprint;
        ActivatedAtUtc = activatedAtUtc;
        ProvisionedByUserId = provisionedByUserId == Guid.Empty ? null : provisionedByUserId;
    }

    /// <summary>Gateway provider identifier the credential belongs to (normalized lowercase).</summary>
    [Required]
    [MaxLength(50)]
    public string ProviderKey { get; private set; } = string.Empty;

    /// <summary>Credential slot on the gateway (normalized lowercase), e.g. <c>api_key</c>.</summary>
    [Required]
    [MaxLength(100)]
    public string SecretName { get; private set; } = string.Empty;

    /// <summary>
    ///     DataProtection-encrypted secret payload (purpose <c>Payments.GatewayCredentials</c>).
    ///     The row never contains the plaintext credential.
    /// </summary>
    [Required]
    public string EncryptedValue { get; private set; } = string.Empty;

    /// <summary>SHA-256 fingerprint (hex prefix) of the plaintext secret, for equality checks without decryption.</summary>
    [Required]
    [MaxLength(64)]
    public string Fingerprint { get; private set; } = string.Empty;

    /// <summary>UTC instant this credential became the active one for its (provider, secret name).</summary>
    public DateTime ActivatedAtUtc { get; private set; }

    /// <summary>UTC instant the credential stopped being active; null while it is the active row.</summary>
    public DateTime? DeactivatedAtUtc { get; private set; }

    /// <summary>Administrator that provisioned the credential; null for system actions.</summary>
    public Guid? ProvisionedByUserId { get; private set; }

    /// <summary>Whether this row is the active credential for its (provider, secret name).</summary>
    public bool IsActive => DeactivatedAtUtc is null;

    /// <summary>
    ///     Deactivates the credential. Idempotent: an already-deactivated row is left
    ///     untouched (the original deactivation instant is preserved).
    /// </summary>
    public void Deactivate(DateTime deactivatedAtUtc)
    {
        if (DeactivatedAtUtc is null)
        {
            DeactivatedAtUtc = deactivatedAtUtc;
            Touch();
        }
    }
}
