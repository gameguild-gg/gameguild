using GameGuild.Infrastructure.Common.ValueObjects;
using GameGuild.Modules.Payments.Models;

namespace GameGuild.Modules.Payments.Services;

/// <summary>
/// Enhanced payment service with advanced processing capabilities
/// </summary>
public interface IAdvancedPaymentService
{
    // Enhanced payment processing
    /// <summary>
    /// Process a payment with full result details
    /// </summary>
    Task<PaymentResult> ProcessPaymentAsync(
        Guid userId,
        Guid? productId,
        Money amount,
        string paymentMethodId,
        string? discountCode = null,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Process a payment for a specific subscription
    /// </summary>
    Task<PaymentResult> ProcessSubscriptionPaymentAsync(
        Guid userId,
        Guid subscriptionId,
        string paymentMethodId,
        CancellationToken cancellationToken = default);

    // Pricing calculations
    /// <summary>
    /// Calculate pricing for a product with discounts and taxes
    /// </summary>
    Task<PricingCalculationResult> CalculatePricingAsync(
        Guid? productId,
        Money baseAmount,
        Guid? userId = null,
        string? discountCode = null,
        string? region = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate pricing for a subscription plan
    /// </summary>
    Task<PricingCalculationResult> CalculateSubscriptionPricingAsync(
        Guid planId,
        Guid? userId = null,
        string? discountCode = null,
        string? billingCycle = null,
        CancellationToken cancellationToken = default);

    // Payment retry and recovery
    /// <summary>
    /// Retry a failed payment
    /// </summary>
    Task<PaymentRetryResult> RetryPaymentAsync(
        Guid paymentId,
        string? newPaymentMethodId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Process automatic retry for failed payments
    /// </summary>
    Task<PaymentRetryResult> ProcessAutomaticRetryAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default);

    // Refunds and reversals
    /// <summary>
    /// Process a refund with detailed result
    /// </summary>
    Task<PaymentResult> ProcessRefundAsync(
        Guid paymentId,
        Money? amount = null,
        string? reason = null,
        bool isPartial = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Process a chargeback
    /// </summary>
    Task<PaymentResult> ProcessChargebackAsync(
        Guid paymentId,
        Money amount,
        string reason,
        string chargebackId,
        CancellationToken cancellationToken = default);

    // Payment method management
    /// <summary>
    /// Validate a payment method before processing
    /// </summary>
    Task<bool> ValidatePaymentMethodAsync(
        string paymentMethodId,
        Guid userId,
        Money? amount = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Setup payment method for future use
    /// </summary>
    Task<string> SetupPaymentMethodAsync(
        Guid userId,
        Dictionary<string, string> paymentMethodData,
        bool saveForFutureUse = true,
        CancellationToken cancellationToken = default);

    // Payment analytics and reporting
    /// <summary>
    /// Get payment statistics for a user
    /// </summary>
    Task<PaymentStatisticsDto> GetUserPaymentStatisticsAsync(
        Guid userId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get payment statistics for admin view
    /// </summary>
    Task<PaymentStatisticsDto> GetPaymentStatisticsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);

    // Webhook and event handling
    /// <summary>
    /// Process webhook from payment provider
    /// </summary>
    Task<bool> ProcessWebhookAsync(
        string provider,
        string eventType,
        Dictionary<string, object> eventData,
        CancellationToken cancellationToken = default);

    // Currency and exchange
    /// <summary>
    /// Convert amount between currencies
    /// </summary>
    Task<Money> ConvertCurrencyAsync(
        Money amount,
        string targetCurrency,
        DateTime? rateDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current exchange rate
    /// </summary>
    Task<decimal> GetExchangeRateAsync(
        string fromCurrency,
        string toCurrency,
        DateTime? rateDate = null,
        CancellationToken cancellationToken = default);

    // Subscription-specific methods
    /// <summary>
    /// Handle subscription renewal payment
    /// </summary>
    Task<PaymentResult> ProcessSubscriptionRenewalAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Handle subscription upgrade/downgrade payment
    /// </summary>
    Task<PaymentResult> ProcessSubscriptionChangeAsync(
        Guid subscriptionId,
        Guid newPlanId,
        bool prorated = true,
        CancellationToken cancellationToken = default);

    // Tax and compliance
    /// <summary>
    /// Calculate tax for a payment
    /// </summary>
    Task<Money> CalculateTaxAsync(
        Money amount,
        string region,
        string? productType = null,
        Guid? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate payment compliance for region
    /// </summary>
    Task<bool> ValidatePaymentComplianceAsync(
        Money amount,
        string paymentMethodId,
        string region,
        Guid userId,
        CancellationToken cancellationToken = default);
}
