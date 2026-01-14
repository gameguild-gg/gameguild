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
        var existingEvent = await _webhookRepository.GetByExternalEventIdAsync(eventId, PaymentProviders.Stripe, cancellationToken).ConfigureAwait(false);
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
            Provider = PaymentProviders.Stripe,
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
    ///     Parses Stripe payload into a common format using System.Text.Json.
    /// </summary>
    private static StripeWebhookPayload ParseStripePayload(string eventType, string payload)
    {
        var result = new StripeWebhookPayload
        {
            EventType = eventType,
            RawPayload = payload
        };

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(payload);
            var root = document.RootElement;

            // Parse common Stripe event structure
            if (root.TryGetProperty("data", out var dataElement) &&
                dataElement.TryGetProperty("object", out var objectElement))
            {
                // Extract subscription-related fields
                if (objectElement.TryGetProperty("id", out var idElement))
                    result.ExternalSubscriptionId = idElement.GetString();

                if (objectElement.TryGetProperty("customer", out var customerElement))
                    result.CustomerId = customerElement.GetString();

                if (objectElement.TryGetProperty("status", out var statusElement))
                    result.Status = statusElement.GetString();

                // Extract metadata for TenantId and PlanId
                if (objectElement.TryGetProperty("metadata", out var metadataElement))
                {
                    if (metadataElement.TryGetProperty("tenant_id", out var tenantIdElement) &&
                        Guid.TryParse(tenantIdElement.GetString(), out var tenantId))
                        result.TenantId = tenantId;

                    if (metadataElement.TryGetProperty("plan_id", out var planIdElement) &&
                        Guid.TryParse(planIdElement.GetString(), out var planId))
                        result.PlanId = planId;
                }

                // Extract subscription/invoice specific fields
                if (objectElement.TryGetProperty("subscription", out var subscriptionElement))
                    result.ExternalSubscriptionId = subscriptionElement.GetString();

                if (objectElement.TryGetProperty("amount_paid", out var amountPaidElement))
                    result.Amount = amountPaidElement.GetDecimal() / 100m; // Stripe uses cents

                if (objectElement.TryGetProperty("amount_due", out var amountDueElement) && !result.Amount.HasValue)
                    result.Amount = amountDueElement.GetDecimal() / 100m;

                if (objectElement.TryGetProperty("currency", out var currencyElement))
                    result.Currency = currencyElement.GetString()?.ToUpperInvariant();

                if (objectElement.TryGetProperty("invoice", out var invoiceElement))
                    result.InvoiceId = invoiceElement.GetString();

                // Extract plan/price info
                if (objectElement.TryGetProperty("items", out var itemsElement) &&
                    itemsElement.TryGetProperty("data", out var itemsDataElement) &&
                    itemsDataElement.GetArrayLength() > 0)
                {
                    var firstItem = itemsDataElement[0];
                    if (firstItem.TryGetProperty("price", out var priceElement))
                    {
                        if (priceElement.TryGetProperty("id", out var priceIdElement))
                            result.PriceId = priceIdElement.GetString();

                        if (priceElement.TryGetProperty("product", out var productElement))
                            result.ProductId = productElement.GetString();
                    }
                }

                // Extract dates
                if (objectElement.TryGetProperty("current_period_start", out var periodStartElement))
                    result.StartDate = DateTimeOffset.FromUnixTimeSeconds(periodStartElement.GetInt64()).UtcDateTime;

                if (objectElement.TryGetProperty("current_period_end", out var periodEndElement))
                    result.EndDate = DateTimeOffset.FromUnixTimeSeconds(periodEndElement.GetInt64()).UtcDateTime;

                if (objectElement.TryGetProperty("billing_cycle_anchor", out var anchorElement))
                    result.NextBillingDate = DateTimeOffset.FromUnixTimeSeconds(anchorElement.GetInt64()).UtcDateTime;
            }
        }
        catch (System.Text.Json.JsonException)
        {
            // If parsing fails, return with raw payload only
            // The caller should handle partial data gracefully
        }

        return result;
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
