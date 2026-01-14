namespace GameGuild.Commerce.Billing;

/// <summary>
///     Service for verifying PayPal webhook signatures using PayPal's API.
/// </summary>
public interface IPayPalSignatureVerificationService
{
    /// <summary>
    ///     Verifies a PayPal webhook signature by calling PayPal's verify-webhook-signature API.
    /// </summary>
    /// <param name="webhookId">Configured webhook ID</param>
    /// <param name="transmissionId">PayPal-Transmission-Id header value</param>
    /// <param name="transmissionTime">PayPal-Transmission-Time header value</param>
    /// <param name="transmissionSig">PayPal-Transmission-Sig header value</param>
    /// <param name="certUrl">PayPal-Cert-Url header value</param>
    /// <param name="authAlgo">PayPal-Auth-Algo header value</param>
    /// <param name="webhookEventBody">Raw webhook event body</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if signature is valid, false otherwise</returns>
    Task<PayPalVerificationResult> VerifySignatureAsync(
        string webhookId,
        string transmissionId,
        string transmissionTime,
        string transmissionSig,
        string? certUrl,
        string? authAlgo,
        string webhookEventBody,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     Result of PayPal webhook signature verification.
/// </summary>
public record PayPalVerificationResult(
    bool IsValid,
    string VerificationStatus,
    string? ErrorMessage = null)
{
    /// <summary>Creates a success result</summary>
    public static PayPalVerificationResult Success() => new(true, "SUCCESS");

    /// <summary>Creates a failure result</summary>
    public static PayPalVerificationResult Failed(string reason) => new(false, "FAILURE", reason);
}
