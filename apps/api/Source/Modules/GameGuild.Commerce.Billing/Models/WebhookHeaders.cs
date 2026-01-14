namespace GameGuild.Commerce.Billing;

/// <summary>
///     Value object containing PayPal webhook verification headers.
///     Extracts data clump from ProcessPayPalWebhookCommand for cleaner API.
/// </summary>
/// <param name="TransmissionId">PayPal transmission ID (used as idempotency key)</param>
/// <param name="TransmissionTime">PayPal transmission timestamp</param>
/// <param name="TransmissionSig">PayPal signature for verification</param>
/// <param name="CertUrl">Optional PayPal certificate URL</param>
/// <param name="AuthAlgo">Optional authentication algorithm</param>
public readonly record struct PayPalWebhookHeaders(
    string TransmissionId,
    string TransmissionTime,
    string TransmissionSig,
    string? CertUrl = null,
    string? AuthAlgo = null)
{
    /// <summary>
    ///     Creates headers from individual HttpContext header values.
    /// </summary>
    public static PayPalWebhookHeaders FromHeaders(
        string transmissionId,
        string transmissionTime,
        string transmissionSig,
        string? certUrl = null,
        string? authAlgo = null)
    {
        return new PayPalWebhookHeaders(
            transmissionId,
            transmissionTime,
            transmissionSig,
            certUrl,
            authAlgo);
    }

    /// <summary>
    ///     Validates that required headers are present.
    /// </summary>
    public bool IsValid =>
        !string.IsNullOrEmpty(TransmissionId) &&
        !string.IsNullOrEmpty(TransmissionTime) &&
        !string.IsNullOrEmpty(TransmissionSig);
}

/// <summary>
///     Value object containing Stripe webhook verification data.
/// </summary>
/// <param name="Signature">Stripe signature header value</param>
/// <param name="WebhookSecret">Webhook endpoint secret for verification</param>
public readonly record struct StripeWebhookHeaders(
    string Signature,
    string? WebhookSecret = null)
{
    /// <summary>
    ///     Validates that required headers are present.
    /// </summary>
    public bool IsValid => !string.IsNullOrEmpty(Signature);
}

/// <summary>
///     Value object containing Apple App Store notification headers.
/// </summary>
/// <param name="SignedPayload">The JWS-signed notification payload</param>
public readonly record struct AppleNotificationHeaders(string SignedPayload)
{
    /// <summary>
    ///     Validates that required data is present.
    /// </summary>
    public bool IsValid => !string.IsNullOrEmpty(SignedPayload);
}
