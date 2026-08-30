namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handles Stripe payment processing, refunds, and webhook validation.
/// </summary>
public interface IStripePaymentService
{
    Task<GatewayPaymentIntentSetupResult> CreatePaymentIntentAsync(
        GatewayPaymentIntentSetupRequest request,
        CancellationToken cancellationToken = default);

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

public sealed record GatewayPaymentIntentSetupRequest(
    string IdempotencyKey,
    decimal Amount,
    string Currency,
    string Description,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record GatewayPaymentIntentSetupResult(
    string? TransactionId,
    PaymentStatus Status,
    string? ClientSecret,
    GatewayProviderMapping? ProviderMapping,
    bool OutcomeUnknown = false,
    string? ErrorCode = null,
    string? ErrorMessage = null);
