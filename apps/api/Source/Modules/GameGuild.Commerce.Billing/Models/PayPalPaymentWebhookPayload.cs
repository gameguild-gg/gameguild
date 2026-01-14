namespace GameGuild.Commerce.Billing;

/// <summary>
///     PayPal-specific payment webhook payload.
///     Concrete implementation of the abstract PaymentWebhookPayload.
/// </summary>
public sealed class PayPalPaymentWebhookPayload : PaymentWebhookPayload
{
    /// <summary>
    ///     PayPal Transaction ID
    /// </summary>
    public string? TransactionId { get; set; }

    /// <summary>
    ///     PayPal Payer ID
    /// </summary>
    public string? PayerId { get; set; }

    /// <summary>
    ///     PayPal Payer Email
    /// </summary>
    public string? PayerEmail { get; set; }

    /// <summary>
    ///     PayPal Capture ID
    /// </summary>
    public string? CaptureId { get; set; }

    /// <summary>
    ///     Whether this is a refund transaction
    /// </summary>
    public bool IsRefund { get; set; }
}
