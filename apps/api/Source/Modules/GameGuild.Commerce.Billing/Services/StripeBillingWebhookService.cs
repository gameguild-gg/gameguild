using GameGuild.Commerce.Subscriptions;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Stripe-specific implementation of the billing webhook service.
///     Handles webhook events from Stripe payment gateway.
/// </summary>
public class StripeBillingWebhookService : BillingWebhookService
{
    private readonly IBillingWebhookRepository _webhookRepository;
    private readonly ILogger<StripeBillingWebhookService> _logger;

    public StripeBillingWebhookService(
        IBillingWebhookRepository webhookRepository,
        ILogger<StripeBillingWebhookService> logger,
        ISubscriptionService subscriptionService) 
        : base(logger, subscriptionService)
    {
        _webhookRepository = webhookRepository;
        _logger = logger;
    }

    /// <summary>
    ///     Process a raw Stripe webhook event with idempotency checking.
    /// </summary>
    /// <param name="eventId">Stripe event ID (used as idempotency key)</param>
    /// <param name="eventType">Stripe event type (e.g., invoice.payment_succeeded)</param>
    /// <param name="payload">Raw JSON payload</param>
    /// <param name="signature">Stripe signature header</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Processing result</returns>
    public async Task<WebhookProcessingResult> ProcessStripeWebhookAsync(
        string eventId,
        string eventType,
        string payload,
        string signature,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing Stripe webhook: {EventType} with ID {EventId}", eventType, eventId);

        // Check for duplicate event (idempotency)
        var existingEvent = await _webhookRepository.GetByExternalEventIdAsync(eventId, "stripe", cancellationToken).ConfigureAwait(false);
        if (existingEvent != null)
        {
            _logger.LogInformation("Duplicate Stripe webhook detected: {EventId}. Returning success.", eventId);
            return WebhookProcessingResult.AlreadyProcessed(eventId, existingEvent.ProcessedAt);
        }

        // Create webhook event record
        // Note: CreatedAt is set automatically by EntityBase
        var webhookEvent = new BillingWebhookEvent
        {
            ExternalEventId = eventId,
            Provider = "stripe",
            EventType = eventType,
            Payload = payload,
            ProcessingAttempts = 1
        };

        try
        {
            // Store the event first (before processing) to handle concurrent retries
            webhookEvent = await _webhookRepository.CreateAsync(webhookEvent, cancellationToken).ConfigureAwait(false);

            // Route to appropriate handler based on event type
            await RouteStripeEventAsync(eventType, payload, cancellationToken).ConfigureAwait(false);

            // Mark as processed
            webhookEvent.MarkAsProcessed();
            await _webhookRepository.UpdateAsync(webhookEvent, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully processed Stripe webhook: {EventId}", eventId);
            return WebhookProcessingResult.Success(eventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Stripe webhook: {EventId}", eventId);

            webhookEvent.MarkAsFailed(ex.Message);
            await _webhookRepository.UpdateAsync(webhookEvent, cancellationToken).ConfigureAwait(false);

            return WebhookProcessingResult.Failed(eventId, ex.Message);
        }
    }

    /// <summary>
    ///     Routes a Stripe event to the appropriate handler based on event type.
    /// </summary>
    private async Task RouteStripeEventAsync(string eventType, string payload, CancellationToken cancellationToken)
    {
        // Parse payload to extract relevant data
        // In production, use Stripe.NET SDK to deserialize properly
        var webhookPayload = ParseStripePayload(eventType, payload);

        switch (eventType)
        {
            case "customer.subscription.created":
                await HandleSubscriptionCreatedAsync(webhookPayload.ToSubscriptionPayload()).ConfigureAwait(false);
                break;

            case "customer.subscription.updated":
                await HandleSubscriptionUpdatedAsync(webhookPayload.ToSubscriptionPayload()).ConfigureAwait(false);
                break;

            case "customer.subscription.deleted":
                await HandleSubscriptionCanceledAsync(webhookPayload.ToSubscriptionPayload()).ConfigureAwait(false);
                break;

            case "invoice.payment_succeeded":
                await HandlePaymentSucceededAsync(webhookPayload.ToPaymentPayload()).ConfigureAwait(false);
                break;

            case "invoice.payment_failed":
                await HandlePaymentFailedAsync(webhookPayload.ToPaymentPayload()).ConfigureAwait(false);
                break;

            default:
                _logger.LogDebug("Unhandled Stripe event type: {EventType}", eventType);
                break;
        }
    }

    /// <summary>
    ///     Parses Stripe payload into a common format.
    /// </summary>
    private static StripeWebhookPayload ParseStripePayload(string eventType, string payload)
    {
        // TODO: Use System.Text.Json or Stripe.NET SDK to properly deserialize
        // This is a placeholder that should be replaced with actual parsing
        return new StripeWebhookPayload
        {
            EventType = eventType,
            RawPayload = payload
        };
    }
}

/// <summary>
///     Internal class for parsing Stripe webhook payloads
/// </summary>
internal class StripeWebhookPayload
{
    public string EventType { get; set; } = string.Empty;
    public string RawPayload { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
    public Guid? PlanId { get; set; }
    public string? ExternalSubscriptionId { get; set; }
    public string? CustomerId { get; set; }
    public string? ProductId { get; set; }
    public string? PriceId { get; set; }
    public string? PaymentId { get; set; }
    public string? InvoiceId { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public string? Status { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? NextBillingDate { get; set; }

    public StripeSubscriptionWebhookPayload ToSubscriptionPayload() => new()
    {
        TenantId = TenantId ?? Guid.Empty,
        PlanId = PlanId ?? Guid.Empty,
        ExternalSubscriptionId = ExternalSubscriptionId ?? string.Empty,
        CustomerId = CustomerId,
        ProductId = ProductId,
        PriceId = PriceId,
        Status = Status ?? string.Empty,
        Amount = Amount ?? 0,
        StartDate = StartDate,
        EndDate = EndDate,
        NextBillingDate = NextBillingDate
    };

    public StripePaymentWebhookPayload ToPaymentPayload() => new()
    {
        TenantId = TenantId ?? Guid.Empty,
        PaymentId = PaymentId ?? string.Empty,
        ExternalSubscriptionId = ExternalSubscriptionId ?? string.Empty,
        CustomerId = CustomerId,
        InvoiceId = InvoiceId,
        Amount = Amount ?? 0,
        Currency = Currency ?? "USD",
        Status = Status ?? string.Empty,
        PaidAt = PaidAt ?? DateTime.UtcNow
    };
}

// WebhookProcessingResult is defined in Models/WebhookProcessingResult.cs
