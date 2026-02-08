using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Stripe customer lifecycle management: customer creation, payment methods, and subscriptions.
/// </summary>
public class StripeCustomerService(
    IOptions<StripeGatewayOptions> options,
    ILogger<StripeCustomerService> logger) : IStripeCustomerService
{
    private readonly StripeGatewayOptions _options = options.Value;
    private readonly CustomerService _customerService = new();
    private readonly PaymentMethodService _paymentMethodService = new();
    private readonly SubscriptionService _subscriptionService = new();

    /// <inheritdoc />
    public async Task<GatewayCustomerResult> CreateCustomerAsync(
        GatewayCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating Stripe customer for email {Email} (Simulation: {IsSimulation})",
            request.Email, _options.UseSimulation);

        if (_options.UseSimulation)
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            return SimulatedPaymentResultFactory.CustomerSuccess(logger);
        }

        try
        {
            var createOptions = new CustomerCreateOptions
            {
                Email = request.Email,
                Name = request.Name,
                Phone = request.Phone,
                Metadata = request.Metadata
            };

            var customer = await _customerService.CreateAsync(
                createOptions, cancellationToken: cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Stripe customer created: {CustomerId}", customer.Id);

            return new GatewayCustomerResult(
                Success: true,
                ExternalCustomerId: customer.Id,
                ErrorCode: null,
                ErrorMessage: null);
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe customer creation failed for email {Email}: {ErrorCode}",
                request.Email, ex.StripeError?.Code);

            return new GatewayCustomerResult(
                Success: false,
                ExternalCustomerId: null,
                ErrorCode: ex.StripeError?.Code ?? "stripe_error",
                ErrorMessage: ex.StripeError?.Message ?? ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during Stripe customer creation for email {Email}", request.Email);
            return SimulatedPaymentResultFactory.CustomerFailure(ex.Message, "unexpected_error");
        }
    }

    /// <inheritdoc />
    public async Task<GatewayPaymentMethodResult> CreatePaymentMethodAsync(
        GatewayPaymentMethodRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Attaching payment method to Stripe customer {CustomerId} (Simulation: {IsSimulation})",
            request.CustomerId, _options.UseSimulation);

        if (_options.UseSimulation)
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            return SimulatedPaymentResultFactory.PaymentMethodSuccess(logger: logger);
        }

        try
        {
            var attachOptions = new PaymentMethodAttachOptions
            {
                Customer = request.CustomerId
            };

            var paymentMethod = await _paymentMethodService.AttachAsync(
                request.PaymentMethodToken,
                attachOptions,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Payment method {PaymentMethodId} attached to customer {CustomerId}",
                paymentMethod.Id, request.CustomerId);

            if (request.SetAsDefault)
            {
                await _customerService.UpdateAsync(
                    request.CustomerId,
                    new CustomerUpdateOptions
                    {
                        InvoiceSettings = new CustomerInvoiceSettingsOptions
                        {
                            DefaultPaymentMethod = paymentMethod.Id
                        }
                    },
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            return new GatewayPaymentMethodResult(
                Success: true,
                ExternalPaymentMethodId: paymentMethod.Id,
                CardLast4: paymentMethod.Card?.Last4,
                CardBrand: paymentMethod.Card?.Brand,
                ExpiryMonth: (int?)paymentMethod.Card?.ExpMonth,
                ExpiryYear: (int?)paymentMethod.Card?.ExpYear,
                ErrorCode: null,
                ErrorMessage: null);
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe payment method attachment failed for customer {CustomerId}: {ErrorCode}",
                request.CustomerId, ex.StripeError?.Code);

            return new GatewayPaymentMethodResult(
                Success: false,
                ExternalPaymentMethodId: null,
                CardLast4: null,
                CardBrand: null,
                ExpiryMonth: null,
                ExpiryYear: null,
                ErrorCode: ex.StripeError?.Code ?? "stripe_error",
                ErrorMessage: ex.StripeError?.Message ?? ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during payment method attachment for customer {CustomerId}",
                request.CustomerId);
            return SimulatedPaymentResultFactory.PaymentMethodFailure(ex.Message, "unexpected_error");
        }
    }

    /// <inheritdoc />
    public async Task<GatewayCancellationResult> CancelSubscriptionAsync(
        string externalSubscriptionId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Cancelling Stripe subscription {SubscriptionId} (Simulation: {IsSimulation})",
            externalSubscriptionId, _options.UseSimulation);

        if (_options.UseSimulation)
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            return SimulatedPaymentResultFactory.CancellationSuccess(logger: logger);
        }

        try
        {
            var subscription = await _subscriptionService.CancelAsync(
                externalSubscriptionId,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Stripe subscription {SubscriptionId} cancelled with status {Status}",
                subscription.Id, subscription.Status);

            return new GatewayCancellationResult(
                Success: subscription.Status == "canceled",
                ErrorCode: null,
                ErrorMessage: null,
                EffectiveDate: subscription.CanceledAt ?? DateTime.UtcNow);
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe subscription cancellation failed for {SubscriptionId}: {ErrorCode}",
                externalSubscriptionId, ex.StripeError?.Code);

            return new GatewayCancellationResult(
                Success: false,
                ErrorCode: ex.StripeError?.Code ?? "stripe_error",
                ErrorMessage: ex.StripeError?.Message ?? ex.Message,
                EffectiveDate: null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during subscription cancellation for {SubscriptionId}",
                externalSubscriptionId);
            return SimulatedPaymentResultFactory.CancellationFailure(ex.Message, "unexpected_error");
        }
    }
}
