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
    ///     Cancels a subscription in Stripe.
    /// </summary>
    Task<GatewayCancellationResult> CancelSubscriptionAsync(
        string externalSubscriptionId,
        CancellationToken cancellationToken = default);
}
