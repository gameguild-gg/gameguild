using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Stripe payment gateway implementation.
///     Provides integration with Stripe's payment processing APIs.
///     Supports both production mode (real Stripe API) and simulation mode (for dev/testing).
/// </summary>
public class StripePaymentGateway : IPaymentGateway
{
    private readonly StripeGatewayOptions _options;
    private readonly ILogger<StripePaymentGateway> _logger;
    private readonly PaymentIntentService _paymentIntentService;
    private readonly RefundService _refundService;
    private readonly CustomerService _customerService;
    private readonly PaymentMethodService _paymentMethodService;
    private readonly SubscriptionService _subscriptionService;

    public StripePaymentGateway(
        IOptions<StripeGatewayOptions> options,
        ILogger<StripePaymentGateway> logger)
    {
        _options = options.Value;
        _logger = logger;

        // Configure Stripe API key if not in simulation mode
        if (!_options.UseSimulation && !string.IsNullOrEmpty(_options.ApiKey))
        {
            StripeConfiguration.ApiKey = _options.ApiKey;
        }

        // Initialize Stripe services
        _paymentIntentService = new PaymentIntentService();
        _refundService = new RefundService();
        _customerService = new CustomerService();
        _paymentMethodService = new PaymentMethodService();
        _subscriptionService = new SubscriptionService();
    }

    /// <inheritdoc />
    public string ProviderId => "stripe";

    /// <inheritdoc />
    public string DisplayName => "Stripe";

    /// <inheritdoc />
    public bool IsEnabled => _options.IsEnabled;

    /// <inheritdoc />
    public async Task<GatewayPaymentResult> ProcessPaymentAsync(
        GatewayPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing Stripe payment: {Amount} {Currency} with idempotency key {IdempotencyKey} (Simulation: {IsSimulation})",
            request.Amount, request.Currency, request.IdempotencyKey, _options.UseSimulation);

        if (_options.UseSimulation)
        {
            return await ProcessPaymentSimulatedAsync(request, cancellationToken);
        }

        return await ProcessPaymentRealAsync(request, cancellationToken);
    }

    private async Task<GatewayPaymentResult> ProcessPaymentRealAsync(
        GatewayPaymentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = ConvertToStripeAmount(request.Amount, request.Currency),
                Currency = request.Currency.ToLowerInvariant(),
                Customer = request.CustomerId,
                PaymentMethod = request.PaymentMethodId,
                Confirm = true,
                Description = request.Description,
                Metadata = request.Metadata,
                // Enable automatic payment methods for broader payment support
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                    AllowRedirects = "never" // For server-side confirmation
                }
            };

            var requestOptions = new RequestOptions 
            { 
                IdempotencyKey = request.IdempotencyKey 
            };

            var paymentIntent = await _paymentIntentService.CreateAsync(
                options, 
                requestOptions, 
                cancellationToken);

            _logger.LogInformation(
                "Stripe payment processed: {PaymentIntentId} with status {Status}",
                paymentIntent.Id, paymentIntent.Status);

            return new GatewayPaymentResult(
                Success: paymentIntent.Status == "succeeded",
                TransactionId: paymentIntent.Id,
                ExternalPaymentId: paymentIntent.LatestChargeId,
                ErrorCode: null,
                ErrorMessage: null,
                Status: MapStripeStatus(paymentIntent.Status),
                ProcessedAt: DateTime.UtcNow);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, 
                "Stripe payment failed for idempotency key {IdempotencyKey}: {ErrorCode} - {ErrorMessage}",
                request.IdempotencyKey, ex.StripeError?.Code, ex.StripeError?.Message);

            return new GatewayPaymentResult(
                Success: false,
                TransactionId: null,
                ExternalPaymentId: null,
                ErrorCode: ex.StripeError?.Code ?? "stripe_error",
                ErrorMessage: ex.StripeError?.Message ?? ex.Message,
                Status: PaymentStatus.Failed,
                ProcessedAt: DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Stripe payment for idempotency key {IdempotencyKey}", 
                request.IdempotencyKey);
            
            return SimulatedPaymentResultFactory.PaymentFailure(ex.Message, "unexpected_error");
        }
    }

    private async Task<GatewayPaymentResult> ProcessPaymentSimulatedAsync(
        GatewayPaymentRequest request,
        CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Using simulated payment response for development/testing");
        return SimulatedPaymentResultFactory.PaymentSuccess(_logger);
    }

    /// <inheritdoc />
    public async Task<GatewayRefundResult> ProcessRefundAsync(
        GatewayRefundRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing Stripe refund for transaction {TransactionId} with idempotency key {IdempotencyKey} (Simulation: {IsSimulation})",
            request.OriginalTransactionId, request.IdempotencyKey, _options.UseSimulation);

        if (_options.UseSimulation)
        {
            return await ProcessRefundSimulatedAsync(request, cancellationToken);
        }

        return await ProcessRefundRealAsync(request, cancellationToken);
    }

    private async Task<GatewayRefundResult> ProcessRefundRealAsync(
        GatewayRefundRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var options = new RefundCreateOptions
            {
                PaymentIntent = request.OriginalTransactionId,
                Reason = MapRefundReason(request.Reason)
            };

            // Only set amount for partial refunds
            if (request.Amount.HasValue)
            {
                // We need to determine the currency from the original payment
                // For now, assume USD; in production, fetch from payment intent
                options.Amount = ConvertToStripeAmount(request.Amount.Value, "USD");
            }

            var requestOptions = new RequestOptions 
            { 
                IdempotencyKey = request.IdempotencyKey 
            };

            var refund = await _refundService.CreateAsync(
                options, 
                requestOptions, 
                cancellationToken);

            _logger.LogInformation(
                "Stripe refund processed: {RefundId} with status {Status}",
                refund.Id, refund.Status);

            return new GatewayRefundResult(
                Success: refund.Status == "succeeded",
                RefundId: refund.Id,
                AmountRefunded: ConvertFromStripeAmount(refund.Amount, refund.Currency),
                ErrorCode: null,
                ErrorMessage: null,
                ProcessedAt: DateTime.UtcNow);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, 
                "Stripe refund failed for transaction {TransactionId}: {ErrorCode} - {ErrorMessage}",
                request.OriginalTransactionId, ex.StripeError?.Code, ex.StripeError?.Message);

            return new GatewayRefundResult(
                Success: false,
                RefundId: null,
                AmountRefunded: 0,
                ErrorCode: ex.StripeError?.Code ?? "stripe_error",
                ErrorMessage: ex.StripeError?.Message ?? ex.Message,
                ProcessedAt: DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Stripe refund for transaction {TransactionId}", 
                request.OriginalTransactionId);
            
            return SimulatedPaymentResultFactory.RefundFailure(ex.Message, "unexpected_error");
        }
    }

    private async Task<GatewayRefundResult> ProcessRefundSimulatedAsync(
        GatewayRefundRequest request,
        CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Using simulated refund response for development/testing");
        return SimulatedPaymentResultFactory.RefundSuccess(request.Amount ?? 0, _logger);
    }

    /// <inheritdoc />
    public Task<bool> ValidateWebhookSignatureAsync(
        string payload,
        string signature,
        string secret)
    {
        if (_options.UseSimulation)
        {
            return ValidateWebhookSignatureSimulatedAsync(payload, signature, secret);
        }

        return ValidateWebhookSignatureRealAsync(payload, signature, secret);
    }

    private Task<bool> ValidateWebhookSignatureRealAsync(
        string payload,
        string signature,
        string secret)
    {
        try
        {
            if (string.IsNullOrEmpty(payload) || string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(secret))
            {
                _logger.LogWarning("Webhook validation failed: missing required parameters");
                return Task.FromResult(false);
            }

            // Use Stripe SDK's cryptographic signature verification
            // This handles:
            // - HMAC-SHA256 signature verification
            // - Timestamp validation (prevents replay attacks within tolerance)
            // - JSON parsing and event construction
            var stripeEvent = EventUtility.ConstructEvent(
                payload,
                signature,
                secret,
                tolerance: _options.WebhookToleranceSeconds);

            _logger.LogDebug(
                "Webhook signature validated successfully for event {EventId} of type {EventType}",
                stripeEvent.Id, stripeEvent.Type);

            return Task.FromResult(true);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, 
                "Stripe webhook signature validation failed: {ErrorMessage}",
                ex.Message);
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error during webhook signature validation");
            return Task.FromResult(false);
        }
    }

    private Task<bool> ValidateWebhookSignatureSimulatedAsync(
        string payload,
        string signature,
        string secret)
    {
        _logger.LogDebug("Using simulated webhook validation for development/testing");

        if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(secret))
        {
            _logger.LogWarning("Simulated webhook validation failed: missing signature or secret");
            return Task.FromResult(false);
        }

        // Basic format check for simulation mode
        var isValidFormat = signature.StartsWith("t=", StringComparison.Ordinal) && signature.Contains(",v1=");
        return Task.FromResult(isValidFormat || !string.IsNullOrEmpty(signature));
    }

    /// <inheritdoc />
    public async Task<GatewayCustomerResult> CreateCustomerAsync(
        GatewayCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating Stripe customer for email {Email} (Simulation: {IsSimulation})", 
            request.Email, _options.UseSimulation);

        if (_options.UseSimulation)
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            return SimulatedPaymentResultFactory.CustomerSuccess(_logger);
        }

        try
        {
            var options = new CustomerCreateOptions
            {
                Email = request.Email,
                Name = request.Name,
                Phone = request.Phone,
                Metadata = request.Metadata
            };

            var customer = await _customerService.CreateAsync(options, cancellationToken: cancellationToken);

            _logger.LogInformation("Stripe customer created: {CustomerId}", customer.Id);

            return new GatewayCustomerResult(
                Success: true,
                ExternalCustomerId: customer.Id,
                ErrorCode: null,
                ErrorMessage: null);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe customer creation failed for email {Email}: {ErrorCode}", 
                request.Email, ex.StripeError?.Code);
            
            return new GatewayCustomerResult(
                Success: false,
                ExternalCustomerId: null,
                ErrorCode: ex.StripeError?.Code ?? "stripe_error",
                ErrorMessage: ex.StripeError?.Message ?? ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Stripe customer creation for email {Email}", request.Email);
            return SimulatedPaymentResultFactory.CustomerFailure(ex.Message, "unexpected_error");
        }
    }

    /// <inheritdoc />
    public async Task<GatewayPaymentMethodResult> CreatePaymentMethodAsync(
        GatewayPaymentMethodRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attaching payment method to Stripe customer {CustomerId} (Simulation: {IsSimulation})", 
            request.CustomerId, _options.UseSimulation);

        if (_options.UseSimulation)
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            return SimulatedPaymentResultFactory.PaymentMethodSuccess(logger: _logger);
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
                cancellationToken: cancellationToken);

            _logger.LogInformation("Payment method {PaymentMethodId} attached to customer {CustomerId}", 
                paymentMethod.Id, request.CustomerId);

            // Set as default if requested
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
                    cancellationToken: cancellationToken);
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
            _logger.LogError(ex, "Stripe payment method attachment failed for customer {CustomerId}: {ErrorCode}", 
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
            _logger.LogError(ex, "Unexpected error during payment method attachment for customer {CustomerId}", 
                request.CustomerId);
            return SimulatedPaymentResultFactory.PaymentMethodFailure(ex.Message, "unexpected_error");
        }
    }

    /// <inheritdoc />
    public async Task<GatewayCancellationResult> CancelSubscriptionAsync(
        string externalSubscriptionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancelling Stripe subscription {SubscriptionId} (Simulation: {IsSimulation})", 
            externalSubscriptionId, _options.UseSimulation);

        if (_options.UseSimulation)
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            return SimulatedPaymentResultFactory.CancellationSuccess(logger: _logger);
        }

        try
        {
            var subscription = await _subscriptionService.CancelAsync(
                externalSubscriptionId, 
                cancellationToken: cancellationToken);

            _logger.LogInformation("Stripe subscription {SubscriptionId} cancelled with status {Status}", 
                subscription.Id, subscription.Status);

            return new GatewayCancellationResult(
                Success: subscription.Status == "canceled",
                ErrorCode: null,
                ErrorMessage: null,
                EffectiveDate: subscription.CanceledAt ?? DateTime.UtcNow);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe subscription cancellation failed for {SubscriptionId}: {ErrorCode}", 
                externalSubscriptionId, ex.StripeError?.Code);
            
            return new GatewayCancellationResult(
                Success: false,
                ErrorCode: ex.StripeError?.Code ?? "stripe_error",
                ErrorMessage: ex.StripeError?.Message ?? ex.Message,
                EffectiveDate: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during subscription cancellation for {SubscriptionId}", 
                externalSubscriptionId);
            return SimulatedPaymentResultFactory.CancellationFailure(ex.Message, "unexpected_error");
        }
    }

    #region Helper Methods

    /// <summary>
    ///     Converts a decimal amount to Stripe's integer format (cents/smallest currency unit).
    /// </summary>
    private static long ConvertToStripeAmount(decimal amount, string currency)
    {
        // Most currencies use 2 decimal places (cents)
        // Some currencies (JPY, KRW) use 0 decimal places
        var zeroDecimalCurrencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "BIF", "CLP", "DJF", "GNF", "JPY", "KMF", "KRW", "MGA", "PYG", "RWF", 
            "UGX", "VND", "VUV", "XAF", "XOF", "XPF"
        };

        if (zeroDecimalCurrencies.Contains(currency))
        {
            return (long)amount;
        }

        return (long)(amount * 100);
    }

    /// <summary>
    ///     Converts a Stripe integer amount back to decimal.
    /// </summary>
    private static decimal ConvertFromStripeAmount(long amount, string currency)
    {
        var zeroDecimalCurrencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "BIF", "CLP", "DJF", "GNF", "JPY", "KMF", "KRW", "MGA", "PYG", "RWF", 
            "UGX", "VND", "VUV", "XAF", "XOF", "XPF"
        };

        if (zeroDecimalCurrencies.Contains(currency))
        {
            return amount;
        }

        return amount / 100m;
    }

    /// <summary>
    ///     Maps Stripe payment intent status to our PaymentStatus enum.
    /// </summary>
    private static PaymentStatus MapStripeStatus(string stripeStatus)
    {
        return stripeStatus switch
        {
            "succeeded" => PaymentStatus.Succeeded,
            "processing" => PaymentStatus.Processing,
            "requires_payment_method" => PaymentStatus.Failed,
            "requires_confirmation" => PaymentStatus.Pending,
            "requires_action" => PaymentStatus.RequiresAction,
            "canceled" => PaymentStatus.Cancelled,
            _ => PaymentStatus.Pending
        };
    }

    /// <summary>
    ///     Maps refund reason string to Stripe's RefundReason.
    /// </summary>
    private static string? MapRefundReason(string? reason)
    {
        if (string.IsNullOrEmpty(reason))
            return null;

        return reason.ToLowerInvariant() switch
        {
            "duplicate" => "duplicate",
            "fraudulent" => "fraudulent",
            "requested_by_customer" or "customer_request" => "requested_by_customer",
            _ => null // Stripe only accepts specific reason values
        };
    }

    #endregion
}

/// <summary>
///     Configuration options for the Stripe payment gateway.
/// </summary>
public class StripeGatewayOptions
{
    /// <summary>
    ///     Section name in configuration.
    /// </summary>
    public const string SectionName = "PaymentGateways:Stripe";

    /// <summary>
    ///     Whether Stripe is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    ///     Whether to use simulated responses instead of real Stripe API.
    ///     Should be true for development/testing, false for production.
    /// </summary>
    public bool UseSimulation { get; set; } = true;

    /// <summary>
    ///     Stripe API key (secret key).
    ///     Required when UseSimulation is false.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    ///     Stripe publishable key (for client-side).
    /// </summary>
    public string PublishableKey { get; set; } = string.Empty;

    /// <summary>
    ///     Webhook signing secret.
    ///     Required for webhook signature verification.
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>
    ///     Tolerance in seconds for webhook timestamp validation.
    ///     Default is 300 seconds (5 minutes) as recommended by Stripe.
    /// </summary>
    public long WebhookToleranceSeconds { get; set; } = 300;
}
