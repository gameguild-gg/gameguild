using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Protects stored gateway payment-method references at rest (issue #414). The
///     <c>GatewayPaymentMethodId</c> column stores a protected payload; plaintext tokens are
///     only materialized when a gateway call or an API response needs them. Values written
///     before this protection carry a <see cref="Marker"/>-less legacy plaintext form and are
///     returned unchanged until the row is next written (dual-format reads).
/// </summary>
public interface IGatewayPaymentMethodIdProtector
{
    /// <summary>Protects a plaintext gateway payment method id for storage.</summary>
    string Protect(string gatewayPaymentMethodId);

    /// <summary>
    ///     Recovers the plaintext gateway payment method id from a stored value. Legacy
    ///     plaintext values (no <see cref="Marker"/>) are returned unchanged.
    /// </summary>
    string Unprotect(string storedGatewayPaymentMethodId);

    /// <summary>Whether a stored value is in the protected format.</summary>
    bool IsProtected(string storedGatewayPaymentMethodId);
}

/// <summary>
///     ASP.NET DataProtection implementation (same pattern as
///     <c>Commerce.Billing/Services/WebhookSigningSecretProvider.cs</c>), purpose
///     <c>Payments.GatewayPaymentMethodIds</c>. Registered as a singleton.
/// </summary>
public sealed class DataProtectionGatewayPaymentMethodIdProtector(IDataProtectionProvider protectionProvider)
    : IGatewayPaymentMethodIdProtector
{
    /// <summary>DataProtection purpose string for gateway payment-method references.</summary>
    public const string ProtectionPurpose = "Payments.GatewayPaymentMethodIds";

    /// <summary>Prefix identifying values stored in the protected format.</summary>
    public const string Marker = "enc1:";

    private readonly IDataProtector _protector = protectionProvider.CreateProtector(ProtectionPurpose);

    /// <inheritdoc />
    public string Protect(string gatewayPaymentMethodId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gatewayPaymentMethodId);
        return Marker + _protector.Protect(gatewayPaymentMethodId.Trim());
    }

    /// <inheritdoc />
    public string Unprotect(string storedGatewayPaymentMethodId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storedGatewayPaymentMethodId);
        return IsProtected(storedGatewayPaymentMethodId)
            ? _protector.Unprotect(storedGatewayPaymentMethodId[Marker.Length..])
            : storedGatewayPaymentMethodId;
    }

    /// <inheritdoc />
    public bool IsProtected(string storedGatewayPaymentMethodId)
        => storedGatewayPaymentMethodId.StartsWith(Marker, StringComparison.Ordinal);
}

/// <summary>
///     Deterministic SHA-256 lookup hash over (gateway provider, gateway payment method id).
///     Not secret: it exists so the registry can find rows by their gateway reference while
///     the stored reference itself stays encrypted (protection is non-deterministic, so
///     ciphertext cannot be queried by equality).
/// </summary>
public static class GatewayPaymentMethodIdHash
{
    /// <summary>Computes the base64 SHA-256 of <c>provider|gatewayId</c> (ordinal UTF-8).</summary>
    public static string Compute(string gatewayProvider, string gatewayPaymentMethodId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gatewayProvider);
        ArgumentException.ThrowIfNullOrWhiteSpace(gatewayPaymentMethodId);

        var payload = Encoding.UTF8.GetBytes($"{gatewayProvider.Trim().ToLowerInvariant()}|{gatewayPaymentMethodId.Trim()}");
        return Convert.ToBase64String(SHA256.HashData(payload));
    }
}
