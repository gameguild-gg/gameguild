using GameGuild.Commerce.Subscriptions;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     PayPal-specific implementation of the billing webhook service.
///     Handles webhook events from PayPal payment gateway (IPN and v2 webhooks).
/// </summary>
public class PayPalBillingWebhookService : BillingWebhookService
{
    private readonly IBillingWebhookRepository _webhookRepository;
    private readonly ILogger<PayPalBillingWebhookService> _logger;

    public PayPalBillingWebhookService(
        IBillingWebhookRepository webhookRepository,
        ILogger<PayPalBillingWebhookService> logger,
        ISubscriptionService subscriptionService)
        : base(logger, subscriptionService)
    {
        _webhookRepository = webhookRepository;
        _logger = logger;
    }

    /// <summary>
    ///     Process a PayPal webhook event with idempotency checking.
    /// </summary>
    /// <param name="webhookId">PayPal webhook ID from transmission headers</param>
    /// <param name="payload">Raw JSON payload</param>
    /// <param name="transmissionId">PayPal transmission ID for verification</param>
    /// <param name="transmissionTime">PayPal transmission time</param>
    /// <param name="transmissionSig">PayPal signature for verification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Processing result</returns>
    public async Task<WebhookProcessingResult> ProcessPayPalWebhookAsync(
        string webhookId,
        string payload,
        string transmissionId,
        string transmissionTime,
        string transmissionSig,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing PayPal webhook: TransmissionId={TransmissionId}", transmissionId);

        // Use transmission ID as idempotency key
        var eventId = transmissionId;

        // Check for duplicate event (idempotency)
        var existingEvent = await _webhookRepository.GetByExternalEventIdAsync(eventId, "paypal", cancellationToken).ConfigureAwait(false);
        if (existingEvent != null)
        {
            _logger.LogInformation("Duplicate PayPal webhook detected: {TransmissionId}. Returning success.", transmissionId);
            return WebhookProcessingResult.AlreadyProcessed(eventId, existingEvent.ProcessedAt);
        }

        // Parse payload to determine event type
        var (eventType, resourceId) = ParsePayPalPayload(payload);

        // Create webhook event record
        var webhookEvent = new BillingWebhookEvent
        {
            ExternalEventId = eventId,
            Provider = "paypal",
            EventType = eventType,
            Payload = payload,
            ProcessingAttempts = 1
        };

        try
        {
            // Store the event first (before processing) to handle concurrent retries
            webhookEvent = await _webhookRepository.CreateAsync(webhookEvent, cancellationToken).ConfigureAwait(false);

            // Verify webhook signature with PayPal
            // Note: In production, call PayPal API to verify the webhook signature
            var isValid = await VerifyPayPalWebhookSignatureAsync(
                webhookId, transmissionId, transmissionTime, transmissionSig, payload, cancellationToken).ConfigureAwait(false);

            if (!isValid)
            {
                webhookEvent.MarkAsFailed("Invalid webhook signature");
                await _webhookRepository.UpdateAsync(webhookEvent, cancellationToken).ConfigureAwait(false);
                return WebhookProcessingResult.Failed(eventId, "Invalid webhook signature");
            }

            // Route to appropriate handler based on event type
            await RoutePayPalEventAsync(eventType, payload, cancellationToken).ConfigureAwait(false);

            // Mark as processed
            webhookEvent.MarkAsProcessed();
            await _webhookRepository.UpdateAsync(webhookEvent, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully processed PayPal webhook: {TransmissionId} ({EventType})", transmissionId, eventType);
            return WebhookProcessingResult.Success(eventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process PayPal webhook: {TransmissionId}", transmissionId);

            webhookEvent.MarkAsFailed(ex.Message);
            await _webhookRepository.UpdateAsync(webhookEvent, cancellationToken).ConfigureAwait(false);

            return WebhookProcessingResult.Failed(eventId, ex.Message);
        }
    }

    /// <summary>
    ///     Verifies PayPal webhook signature.
    ///     In production, this should call PayPal's verify-webhook-signature API.
    /// </summary>
    private async Task<bool> VerifyPayPalWebhookSignatureAsync(
        string webhookId,
        string transmissionId,
        string transmissionTime,
        string transmissionSig,
        string payload,
        CancellationToken cancellationToken)
    {
        // Production implementation would:
        // 1. Call PayPal API: POST /v1/notifications/verify-webhook-signature
        // 2. Pass: webhook_id, transmission_id, transmission_time, transmission_sig, webhook_event
        // 3. Verify response verification_status == "SUCCESS"
        
        _logger.LogDebug(
            "Verifying PayPal webhook signature. WebhookId={WebhookId}, TransmissionId={TransmissionId}",
            webhookId, transmissionId);

        // For now, return true if all required fields are present
        // TODO: Implement actual PayPal API verification
        await Task.CompletedTask;
        return !string.IsNullOrEmpty(transmissionId) && 
               !string.IsNullOrEmpty(transmissionSig) &&
               !string.IsNullOrEmpty(payload);
    }

    /// <summary>
    ///     Routes a PayPal event to the appropriate handler based on event type.
    /// </summary>
    private async Task RoutePayPalEventAsync(string eventType, string payload, CancellationToken cancellationToken)
    {
        var webhookPayload = ParsePayPalPayloadData(payload);

        switch (eventType)
        {
            // Subscription events
            case "BILLING.SUBSCRIPTION.CREATED":
                await HandleSubscriptionCreatedAsync(webhookPayload.ToSubscriptionPayload()).ConfigureAwait(false);
                break;

            case "BILLING.SUBSCRIPTION.ACTIVATED":
            case "BILLING.SUBSCRIPTION.UPDATED":
                await HandleSubscriptionUpdatedAsync(webhookPayload.ToSubscriptionPayload()).ConfigureAwait(false);
                break;

            case "BILLING.SUBSCRIPTION.CANCELLED":
            case "BILLING.SUBSCRIPTION.SUSPENDED":
            case "BILLING.SUBSCRIPTION.EXPIRED":
                await HandleSubscriptionCanceledAsync(webhookPayload.ToSubscriptionPayload()).ConfigureAwait(false);
                break;

            // Payment events
            case "PAYMENT.SALE.COMPLETED":
            case "PAYMENT.CAPTURE.COMPLETED":
                await HandlePaymentSucceededAsync(webhookPayload.ToPaymentPayload()).ConfigureAwait(false);
                break;

            case "PAYMENT.SALE.DENIED":
            case "PAYMENT.SALE.REFUNDED":
            case "PAYMENT.CAPTURE.DENIED":
                await HandlePaymentFailedAsync(webhookPayload.ToPaymentPayload()).ConfigureAwait(false);
                break;

            default:
                _logger.LogDebug("Unhandled PayPal event type: {EventType}", eventType);
                break;
        }
    }

    /// <summary>
    ///     Parses PayPal payload to extract event type and resource ID.
    /// </summary>
    private static (string eventType, string resourceId) ParsePayPalPayload(string payload)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            var eventType = root.TryGetProperty("event_type", out var typeProp) ? typeProp.GetString() : null;
            
            var resourceId = string.Empty;
            if (root.TryGetProperty("resource", out var resource))
            {
                resourceId = resource.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? string.Empty : string.Empty;
            }

            return (eventType ?? "unknown", resourceId);
        }
        catch
        {
            return ("unknown", string.Empty);
        }
    }

    /// <summary>
    ///     Parses PayPal payload into structured data.
    /// </summary>
    private static PayPalWebhookPayload ParsePayPalPayloadData(string payload)
    {
        var result = new PayPalWebhookPayload();

        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            result.EventType = root.TryGetProperty("event_type", out var typeProp) ? typeProp.GetString() ?? string.Empty : string.Empty;

            if (root.TryGetProperty("resource", out var resource))
            {
                result.ResourceId = resource.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                result.Status = resource.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : null;

                // Parse billing agreement / subscription data
                if (resource.TryGetProperty("billing_agreement_id", out var agreementProp))
                {
                    result.SubscriptionId = agreementProp.GetString();
                }
                
                // Parse amount
                if (resource.TryGetProperty("amount", out var amount))
                {
                    if (amount.TryGetProperty("total", out var totalProp) && 
                        decimal.TryParse(totalProp.GetString(), out var total))
                    {
                        result.Amount = total;
                    }
                    result.Currency = amount.TryGetProperty("currency", out var currProp) ? currProp.GetString() : "USD";
                }
            }
        }
        catch
        {
            // Return empty payload on parse failure
        }

        return result;
    }
}

/// <summary>
///     Internal class for parsing PayPal webhook payloads
/// </summary>
internal class PayPalWebhookPayload
{
    public string EventType { get; set; } = string.Empty;
    public string? ResourceId { get; set; }
    public string? SubscriptionId { get; set; }
    public string? CustomerId { get; set; }
    public string? PaymentId { get; set; }
    public string? InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
    public string? Status { get; set; }
    public Guid TenantId { get; set; }
    public Guid PlanId { get; set; }

    public PayPalSubscriptionWebhookPayload ToSubscriptionPayload() => new()
    {
        TenantId = TenantId,
        PlanId = PlanId,
        ExternalSubscriptionId = SubscriptionId ?? ResourceId ?? string.Empty,
        Status = Status ?? string.Empty,
        Amount = Amount,
        StartDate = DateTime.UtcNow,
        EndDate = null,
        PayerId = CustomerId,
        BillingAgreementId = SubscriptionId
    };

    public PayPalPaymentWebhookPayload ToPaymentPayload() => new()
    {
        TenantId = TenantId,
        PaymentId = PaymentId ?? ResourceId ?? string.Empty,
        ExternalSubscriptionId = SubscriptionId ?? string.Empty,
        Amount = Amount,
        Currency = Currency ?? "USD",
        PaidAt = DateTime.UtcNow,
        FailureReason = null,
        TransactionId = PaymentId,
        PayerId = CustomerId
    };
}
