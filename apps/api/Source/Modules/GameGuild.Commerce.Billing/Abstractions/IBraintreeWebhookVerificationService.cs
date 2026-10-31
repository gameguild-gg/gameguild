namespace GameGuild.Commerce.Billing;

/// <summary>
///     Verifies Braintree webhook deliveries for the billing ingress (issue #408).
///     Braintree POSTs <c>bt_signature</c> and <c>bt_payload</c> form fields; the signature is
///     <c>{public key}|{sha1 hex of (public key + "|" + bt_payload)}</c>. Verification is
///     offline and constant-time on the digest comparison — no provider API round-trip.
/// </summary>
public interface IBraintreeWebhookVerificationService
{
    /// <summary>
    ///     Verifies the <c>bt_signature</c> against the configured public key and the raw
    ///     <c>bt_payload</c>, then decodes the payload into the typed notification.
    /// </summary>
    /// <param name="btSignature">Raw <c>bt_signature</c> form field value.</param>
    /// <param name="btPayload">Raw <c>bt_payload</c> form field value (base64).</param>
    /// <returns>The verification outcome with the decoded notification on success.</returns>
    BraintreeWebhookVerificationResult Verify(string btSignature, string btPayload);
}

/// <summary>Outcome of a Braintree webhook verification.</summary>
/// <param name="IsValid">Whether the signature verified against the configured public key.</param>
/// <param name="Notification">Decoded notification when <paramref name="IsValid"/> is true.</param>
/// <param name="ErrorMessage">Failure reason when <paramref name="IsValid"/> is false.</param>
public sealed record BraintreeWebhookVerificationResult(
    bool IsValid,
    BraintreeWebhookNotification? Notification = null,
    string? ErrorMessage = null)
{
    /// <summary>Creates a success result carrying the decoded notification.</summary>
    public static BraintreeWebhookVerificationResult Success(BraintreeWebhookNotification notification) =>
        new(true, notification);

    /// <summary>Creates a failure result; the reason never discloses key material.</summary>
    public static BraintreeWebhookVerificationResult Failed(string reason) => new(false, null, reason);
}
