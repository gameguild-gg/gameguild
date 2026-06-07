namespace GameGuild.Commerce.Payments;

/// <summary>
///     Service for processing payments and managing billing
/// </summary>
public interface IPaymentService
{
    /// <summary>
    ///     Processes a payment for a subscription
    /// </summary>
    Task<PaymentResult> ProcessPaymentAsync(Guid tenantId, Guid subscriptionId, decimal amount, string paymentMethodId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Calculates pricing for a subscription plan
    /// </summary>
    Task<PricingCalculationResult> CalculatePricingAsync(Guid planId, Guid? tenantId = null, string? discountCode = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retries a failed payment
    /// </summary>
    Task<PaymentRetryResult> RetryPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Processes a refund
    /// </summary>
    Task<PaymentResult> ProcessRefundAsync(Guid paymentId, decimal? amount = null, string? reason = null, CancellationToken cancellationToken = default);
}
