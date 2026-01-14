using GameGuild.Commerce.Subscriptions;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Apple Pay-specific implementation of the billing webhook service.
///     Handles webhook events from Apple Pay payment gateway.
/// </summary>
public class ApplePayBillingWebhookService : BillingWebhookService
{
    private readonly IBillingWebhookRepository _webhookRepository;
    private readonly ILogger<ApplePayBillingWebhookService> _logger;

    public ApplePayBillingWebhookService(
        IBillingWebhookRepository webhookRepository,
        ILogger<ApplePayBillingWebhookService> logger,
        ISubscriptionService subscriptionService)
        : base(logger, subscriptionService)
    {
        _webhookRepository = webhookRepository;
        _logger = logger;
    }

    /// <summary>
    ///     Process an Apple Pay webhook event with idempotency checking.
    /// </summary>
    /// <param name="payload">Raw JSON payload</param>
    /// <param name="merchantId">Apple Pay merchant ID from headers</param>
    /// <param name="signature">Apple Pay signature for verification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Processing result</returns>
    public async Task<WebhookProcessingResult> ProcessApplePayWebhookAsync(
        string payload,
        string merchantId,
        string signature,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing Apple Pay webhook for merchant: {MerchantId}", merchantId);

        // Parse payload to extract event info
        var (eventId, eventType, transactionId) = ParseApplePayPayload(payload);

        if (string.IsNullOrEmpty(eventId))
        {
            // Generate event ID from transaction ID if not present
            eventId = $"apple_{transactionId ?? Guid.NewGuid().ToString()}";
        }

        // Check for duplicate event (idempotency)
        var existingEvent = await _webhookRepository.GetByExternalEventIdAsync(eventId, "apple_pay", cancellationToken).ConfigureAwait(false);
        if (existingEvent != null)
        {
            _logger.LogInformation("Duplicate Apple Pay webhook detected: {EventId}. Returning success.", eventId);
            return WebhookProcessingResult.AlreadyProcessed(eventId, existingEvent.ProcessedAt);
        }

        // Create webhook event record
        var webhookEvent = new BillingWebhookEvent
        {
            ExternalEventId = eventId,
            Provider = "apple_pay",
            EventType = eventType,
            Payload = payload,
            ProcessingAttempts = 1
        };

        try
        {
            // Store the event first (before processing) to handle concurrent retries
            webhookEvent = await _webhookRepository.CreateAsync(webhookEvent, cancellationToken).ConfigureAwait(false);

            // Verify merchant ID
            var isValid = await VerifyApplePayMerchantAsync(merchantId, signature, payload, cancellationToken).ConfigureAwait(false);

            if (!isValid)
            {
                webhookEvent.MarkAsFailed("Invalid merchant ID or signature");
                await _webhookRepository.UpdateAsync(webhookEvent, cancellationToken).ConfigureAwait(false);
                return WebhookProcessingResult.Failed(eventId, "Invalid merchant ID or signature");
            }

            // Route to appropriate handler based on event type
            await RouteApplePayEventAsync(eventType, payload, cancellationToken).ConfigureAwait(false);

            // Mark as processed
            webhookEvent.MarkAsProcessed();
            await _webhookRepository.UpdateAsync(webhookEvent, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully processed Apple Pay webhook: {EventId} ({EventType})", eventId, eventType);
            return WebhookProcessingResult.Success(eventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Apple Pay webhook: {EventId}", eventId);

            webhookEvent.MarkAsFailed(ex.Message);
            await _webhookRepository.UpdateAsync(webhookEvent, cancellationToken).ConfigureAwait(false);

            return WebhookProcessingResult.Failed(eventId, ex.Message);
        }
    }

    /// <summary>
    ///     Verifies Apple Pay merchant and signature.
    /// </summary>
    private async Task<bool> VerifyApplePayMerchantAsync(
        string merchantId,
        string signature,
        string payload,
        CancellationToken cancellationToken)
    {
        // Production implementation would:
        // 1. Verify merchant ID is in allowed list
        // 2. Verify signature using Apple's certificate chain
        // 3. Validate payment data format
        
        _logger.LogDebug("Verifying Apple Pay webhook. MerchantId={MerchantId}", merchantId);

        // TODO: Implement actual Apple Pay verification
        await Task.CompletedTask;
        return !string.IsNullOrEmpty(merchantId) && 
               !string.IsNullOrEmpty(signature) &&
               !string.IsNullOrEmpty(payload);
    }

    /// <summary>
    ///     Routes an Apple Pay event to the appropriate handler based on event type.
    /// </summary>
    private async Task RouteApplePayEventAsync(string eventType, string payload, CancellationToken cancellationToken)
    {
        var webhookPayload = ParseApplePayPayloadData(payload);

        switch (eventType.ToLowerInvariant())
        {
            // Payment events
            case "payment_authorized":
            case "payment_completed":
                await HandlePaymentSucceededAsync(webhookPayload.ToPaymentPayload()).ConfigureAwait(false);
                break;

            case "payment_declined":
            case "payment_cancelled":
                await HandlePaymentFailedAsync(webhookPayload.ToPaymentPayload()).ConfigureAwait(false);
                break;

            // Subscription events (if using Apple subscriptions)
            case "subscription_created":
                await HandleSubscriptionCreatedAsync(webhookPayload.ToSubscriptionPayload()).ConfigureAwait(false);
                break;

            case "subscription_renewed":
            case "subscription_updated":
                await HandleSubscriptionUpdatedAsync(webhookPayload.ToSubscriptionPayload()).ConfigureAwait(false);
                break;

            case "subscription_cancelled":
            case "subscription_expired":
                await HandleSubscriptionCanceledAsync(webhookPayload.ToSubscriptionPayload()).ConfigureAwait(false);
                break;

            default:
                _logger.LogDebug("Unhandled Apple Pay event type: {EventType}", eventType);
                break;
        }
    }

    /// <summary>
    ///     Parses Apple Pay payload to extract event info.
    /// </summary>
    private static (string eventId, string eventType, string? transactionId) ParseApplePayPayload(string payload)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            var eventId = root.TryGetProperty("eventId", out var idProp) ? idProp.GetString() : null;
            var eventType = root.TryGetProperty("eventType", out var typeProp) ? typeProp.GetString() : null;
            var transactionId = root.TryGetProperty("transactionId", out var txProp) ? txProp.GetString() : null;

            // Also check nested payment data
            if (root.TryGetProperty("payment", out var payment))
            {
                transactionId ??= payment.TryGetProperty("transactionIdentifier", out var txIdProp) ? txIdProp.GetString() : null;
            }

            return (eventId ?? string.Empty, eventType ?? "unknown", transactionId);
        }
        catch
        {
            return (string.Empty, "unknown", null);
        }
    }

    /// <summary>
    ///     Parses Apple Pay payload into structured data.
    /// </summary>
    private static ApplePayWebhookPayload ParseApplePayPayloadData(string payload)
    {
        var result = new ApplePayWebhookPayload();

        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            result.EventType = root.TryGetProperty("eventType", out var typeProp) ? typeProp.GetString() ?? string.Empty : string.Empty;
            result.TransactionId = root.TryGetProperty("transactionId", out var txProp) ? txProp.GetString() : null;

            // Parse payment data
            if (root.TryGetProperty("payment", out var payment))
            {
                result.TransactionId ??= payment.TryGetProperty("transactionIdentifier", out var txIdProp) ? txIdProp.GetString() : null;
                
                if (payment.TryGetProperty("token", out var token))
                {
                    if (token.TryGetProperty("paymentData", out var paymentData))
                    {
                        // Parse payment data if present
                    }
                }
            }

            // Parse amount
            if (root.TryGetProperty("amount", out var amount))
            {
                if (decimal.TryParse(amount.GetString(), out var amountValue))
                {
                    result.Amount = amountValue;
                }
            }

            result.Currency = root.TryGetProperty("currency", out var currProp) ? currProp.GetString() : "USD";
        }
        catch
        {
            // Return empty payload on parse failure
        }

        return result;
    }
}

/// <summary>
///     Internal class for parsing Apple Pay webhook payloads
/// </summary>
internal class ApplePayWebhookPayload
{
    public string EventType { get; set; } = string.Empty;
    public string? TransactionId { get; set; }
    public string? SubscriptionId { get; set; }
    public string? CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
    public string? Status { get; set; }
    public Guid TenantId { get; set; }
    public Guid PlanId { get; set; }

    public ApplePaySubscriptionWebhookPayload ToSubscriptionPayload() => new()
    {
        TenantId = TenantId,
        PlanId = PlanId,
        ExternalSubscriptionId = SubscriptionId ?? TransactionId ?? string.Empty,
        Status = Status ?? string.Empty,
        Amount = Amount,
        StartDate = DateTime.UtcNow,
        EndDate = null,
        OriginalTransactionId = TransactionId
    };

    public ApplePayPaymentWebhookPayload ToPaymentPayload() => new()
    {
        TenantId = TenantId,
        PaymentId = TransactionId ?? string.Empty,
        ExternalSubscriptionId = SubscriptionId ?? string.Empty,
        Amount = Amount,
        Currency = Currency ?? "USD",
        PaidAt = DateTime.UtcNow,
        FailureReason = null,
        TransactionId = TransactionId,
        OriginalTransactionId = TransactionId
    };
}
