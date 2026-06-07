namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handles Stripe customer lifecycle: creation, payment methods, and subscriptions.
/// </summary>
public interface IStripeCustomerService
{
    /// <summary>
    ///     Creates a customer in Stripe.
    /// </summary>
    Task<GatewayCustomerResult> CreateCustomerAsync(
        GatewayCustomerRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Attaches a payment method to a Stripe customer.
    /// </summary>
    Task<GatewayPaymentMethodResult> CreatePaymentMethodAsync(
        GatewayPaymentMethodRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Creates a Stripe SetupIntent for collecting a reusable payment method.
    /// </summary>
    Task<GatewaySetupIntentResult> CreateSetupIntentAsync(
        GatewaySetupIntentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sets the default payment method for a Stripe customer.
    /// </summary>
    Task<GatewayDefaultPaymentMethodResult> SetDefaultPaymentMethodAsync(
        GatewayDefaultPaymentMethodRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Cancels a subscription in Stripe.
    /// </summary>
    Task<GatewayCancellationResult> CancelSubscriptionAsync(
        string externalSubscriptionId,
        CancellationToken cancellationToken = default);
}

public sealed record GatewaySetupIntentRequest(
    string CustomerId,
    Dictionary<string, string>? Metadata = null);

public sealed record GatewaySetupIntentResult(
    bool Success,
    string? ExternalSetupIntentId,
    string? ClientSecret,
    string? CustomerId,
    string? ErrorCode,
    string? ErrorMessage);

public sealed record GatewayDefaultPaymentMethodRequest(
    string CustomerId,
    string PaymentMethodId);

public sealed record GatewayDefaultPaymentMethodResult(
    bool Success,
    string? ErrorCode,
    string? ErrorMessage);
