namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handles Stripe payment processing, refunds, and webhook validation.
/// </summary>
public interface IStripePaymentService
{
    /// <summary>
    ///     Processes a payment through Stripe.
    /// </summary>
    Task<GatewayPaymentResult> ProcessPaymentAsync(
        GatewayPaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<GatewayPaymentResult> GetPaymentAsync(
        string externalTransactionId,
        CancellationToken cancellationToken = default);

    Task<GatewayPaymentCancellationResult> CancelPaymentAsync(
        string externalTransactionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Processes a refund through Stripe.
    /// </summary>
    Task<GatewayRefundResult> ProcessRefundAsync(
        GatewayRefundRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Validates a Stripe webhook signature.
    /// </summary>
    Task<bool> ValidateWebhookSignatureAsync(
        string payload,
        string signature,
        string secret);
}
