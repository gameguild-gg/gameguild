namespace GameGuild.Commerce.Billing;

/// <summary>
///     Apple Pay (App Store Server Notifications) payment webhook payload.
///     Concrete implementation of the abstract PaymentWebhookPayload.
/// </summary>
public sealed class ApplePayPaymentWebhookPayload : PaymentWebhookPayload
{
    /// <summary>
    ///     Apple Transaction ID
    /// </summary>
    public string? TransactionId { get; set; }

    /// <summary>
    ///     Apple Original Transaction ID
    /// </summary>
    public string? OriginalTransactionId { get; set; }

    /// <summary>
    ///     Apple Product ID
    /// </summary>
    public string? ProductId { get; set; }

    /// <summary>
    ///     Web Order Line Item ID
    /// </summary>
    public string? WebOrderLineItemId { get; set; }

    /// <summary>
    ///     App Store environment (Production, Sandbox)
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    ///     Whether this is a refund
    /// </summary>
    public bool IsRefund { get; set; }

    /// <summary>
    ///     Revocation date if subscription was refunded
    /// </summary>
    public DateTime? RevocationDate { get; set; }
}
