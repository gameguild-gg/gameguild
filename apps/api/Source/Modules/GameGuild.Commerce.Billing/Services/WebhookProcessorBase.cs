using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Base class for webhook processors implementing Template Method pattern.
///     Encapsulates the common flow: validate signature → check idempotency → store event → process → update status.
/// </summary>
/// <remarks>
///     Provider-specific implementations should override the abstract/virtual methods to customize behavior.
/// </remarks>
public abstract class WebhookProcessorBase
{
    private readonly IBillingWebhookRepository _webhookRepository;
    private readonly ILogger _logger;

    /// <summary>
    ///     The payment provider identifier (e.g., PaymentProviders.Stripe).
    /// </summary>
    protected abstract string ProviderName { get; }

    protected WebhookProcessorBase(
        IBillingWebhookRepository webhookRepository,
        ILogger logger)
    {
        _webhookRepository = webhookRepository;
        _logger = logger;
    }

    /// <summary>
    ///     Template Method: Process a webhook with standard idempotency and error handling.
    /// </summary>
    /// <param name="eventId">Unique event identifier for idempotency</param>
    /// <param name="eventType">The event type (e.g., customer.subscription.created)</param>
    /// <param name="payload">Raw payload string</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Processing result</returns>
    protected async Task<WebhookProcessingResult> ProcessWebhookAsync(
        string eventId,
        string eventType,
        string payload,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing {Provider} webhook: {EventType} with ID {EventId}",
            ProviderName, eventType, eventId);

        // Step 1: Check for duplicate event (idempotency)
        var existingEvent = await _webhookRepository
            .GetByExternalEventIdAsync(eventId, ProviderName, cancellationToken)
            .ConfigureAwait(false);

        if (existingEvent != null)
        {
            _logger.LogInformation(
                "Duplicate {Provider} webhook detected: {EventId}. Returning success.",
                ProviderName, eventId);
            return WebhookProcessingResult.AlreadyProcessed(eventId, existingEvent.ProcessedAt);
        }

        // Step 2: Create webhook event record
        var webhookEvent = new BillingWebhookEvent
        {
            ExternalEventId = eventId,
            Provider = ProviderName,
            EventType = eventType,
            Payload = payload,
            ProcessingAttempts = 1
        };

        try
        {
            // Step 3: Store the event first (before processing) to handle concurrent retries
            webhookEvent = await _webhookRepository
                .CreateAsync(webhookEvent, cancellationToken)
                .ConfigureAwait(false);

            // Step 4: Route to provider-specific handler
            await RouteEventAsync(eventType, payload, cancellationToken).ConfigureAwait(false);

            // Step 5: Mark as processed
            webhookEvent.MarkAsProcessed();
            await _webhookRepository.UpdateAsync(webhookEvent, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Successfully processed {Provider} webhook: {EventId}",
                ProviderName, eventId);
            return WebhookProcessingResult.Success(eventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process {Provider} webhook: {EventId}",
                ProviderName, eventId);

            webhookEvent.MarkAsFailed(ex.Message);
            await _webhookRepository.UpdateAsync(webhookEvent, cancellationToken).ConfigureAwait(false);

            return WebhookProcessingResult.Failed(eventId, ex.Message);
        }
    }

    /// <summary>
    ///     Routes the event to the appropriate handler based on event type.
    ///     Override in derived classes to implement provider-specific routing.
    /// </summary>
    /// <param name="eventType">The event type</param>
    /// <param name="payload">The raw payload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    protected abstract Task RouteEventAsync(
        string eventType,
        string payload,
        CancellationToken cancellationToken);
}

/// <summary>
///     Event type constants for common webhook events across providers.
/// </summary>
public static class WebhookEventTypes
{
    /// <summary>Stripe event types</summary>
    public static class Stripe
    {
        public const string SubscriptionCreated = "customer.subscription.created";
        public const string SubscriptionUpdated = "customer.subscription.updated";
        public const string SubscriptionDeleted = "customer.subscription.deleted";
        public const string InvoicePaymentSucceeded = "invoice.payment_succeeded";
        public const string InvoicePaymentFailed = "invoice.payment_failed";
        public const string PaymentIntentSucceeded = "payment_intent.succeeded";
        public const string PaymentIntentFailed = "payment_intent.payment_failed";
    }

    /// <summary>PayPal event types</summary>
    public static class PayPal
    {
        public const string SubscriptionCreated = "BILLING.SUBSCRIPTION.CREATED";
        public const string SubscriptionActivated = "BILLING.SUBSCRIPTION.ACTIVATED";
        public const string SubscriptionUpdated = "BILLING.SUBSCRIPTION.UPDATED";
        public const string SubscriptionCancelled = "BILLING.SUBSCRIPTION.CANCELLED";
        public const string SubscriptionSuspended = "BILLING.SUBSCRIPTION.SUSPENDED";
        public const string SubscriptionExpired = "BILLING.SUBSCRIPTION.EXPIRED";
        public const string PaymentSaleCompleted = "PAYMENT.SALE.COMPLETED";
        public const string PaymentCaptureCompleted = "PAYMENT.CAPTURE.COMPLETED";
        public const string PaymentCaptureDenied = "PAYMENT.CAPTURE.DENIED";
    }

    /// <summary>Apple App Store notification types</summary>
    public static class Apple
    {
        public const string SubscribedInitial = "SUBSCRIBED";
        public const string Renewed = "DID_RENEW";
        public const string FailedToRenew = "DID_FAIL_TO_RENEW";
        public const string Expired = "EXPIRED";
        public const string Refunded = "REFUND";
        public const string GracePeriodExpired = "GRACE_PERIOD_EXPIRED";
        public const string Revoked = "REVOKE";
    }
}
