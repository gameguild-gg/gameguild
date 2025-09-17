using System.Text.Json;
using GameGuild.Database;
using GameGuild.Modules.Billing.Models;
using GameGuild.Modules.Payments;
using GameGuild.Modules.Subscriptions.Services;


namespace GameGuild.Modules.Billing.Services;

/// <summary>
/// Service for handling billing webhooks from various providers
/// </summary>
public class BillingWebhookService : IBillingWebhookService {
  private readonly ApplicationDbContext _context;

  private readonly ILogger<BillingWebhookService> _logger;

  private readonly ISubscriptionService? _subscriptionService;

  private readonly IPaymentService? _paymentService;

  public BillingWebhookService(ApplicationDbContext context, ILogger<BillingWebhookService> logger, ISubscriptionService? subscriptionService = null, IPaymentService? paymentService = null) {
    _context = context;
    _logger = logger;
    _subscriptionService = subscriptionService;
    _paymentService = paymentService;
  }

  public async Task<WebhookProcessingResult> ProcessWebhookAsync(string provider, string payload, Dictionary<string, string> headers, CancellationToken cancellationToken = default) {
    try {
      // Extract event info from payload
      var eventId = ExtractEventId(payload, provider);
      var eventType = ExtractEventType(payload, provider);

      if (string.IsNullOrEmpty(eventId) || string.IsNullOrEmpty(eventType)) { return WebhookProcessingResult.Failure("Could not extract event ID or type from payload"); }

      // Check if we've already processed this webhook
      var existingEvent = await GetWebhookEventAsync(provider, eventId, cancellationToken);

      if (existingEvent?.IsProcessed == true) {
        _logger.LogInformation("Webhook {Provider}:{EventId} already processed", provider, eventId);

        return WebhookProcessingResult.Success();
      }

      // Create or update webhook event record
      var webhookEvent = existingEvent ?? new BillingWebhookEvent { Provider = provider, ExternalEventId = eventId, EventType = eventType, Payload = payload, Headers = JsonSerializer.Serialize(headers) };

      if (existingEvent == null) { _context.Set<BillingWebhookEvent>().Add(webhookEvent); }

      webhookEvent.IncrementAttempts();

      // Process based on provider
      var result = provider.ToLowerInvariant() switch {
        "stripe" => await ProcessStripeEvent(webhookEvent, cancellationToken), "paypal" => await ProcessPayPalEvent(webhookEvent, cancellationToken), _ => WebhookProcessingResult.Failure($"Unsupported provider: {provider}")
      };

      if (result.IsSuccess) { webhookEvent.MarkAsProcessed(); }
      else { webhookEvent.MarkAsFailed(result.ErrorMessage ?? "Unknown error"); }

      await _context.SaveChangesAsync(cancellationToken);

      return result;
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error processing webhook from {Provider}", provider);

      return WebhookProcessingResult.Failure($"Processing error: {ex.Message}");
    }
  }

  public async Task<WebhookProcessingResult> ProcessStripeWebhookAsync(string payload, string signatureHeader, CancellationToken cancellationToken = default) {
    // TODO: Verify Stripe signature
    var headers = new Dictionary<string, string> { ["Stripe-Signature"] = signatureHeader };

    return await ProcessWebhookAsync("stripe", payload, headers, cancellationToken);
  }

  public async Task<WebhookProcessingResult> ProcessPayPalWebhookAsync(string payload, Dictionary<string, string> headers, CancellationToken cancellationToken = default) {
    return await ProcessWebhookAsync("paypal", payload, headers, cancellationToken);
  }

  public async Task<BillingWebhookEvent?> GetWebhookEventAsync(string provider, string externalEventId, CancellationToken cancellationToken = default) {
    return await _context.Set<BillingWebhookEvent>().FirstOrDefaultAsync(e => e.Provider == provider && e.ExternalEventId == externalEventId, cancellationToken);
  }

  public async Task<IEnumerable<BillingWebhookEvent>> GetWebhookEventsAsync(Guid? tenantId = null, Guid? subscriptionId = null, Guid? userId = null, CancellationToken cancellationToken = default) {
    var query = _context.Set<BillingWebhookEvent>().AsQueryable();

    if (tenantId.HasValue) query = query.Where(e => e.TenantId == tenantId.Value);

    if (subscriptionId.HasValue) query = query.Where(e => e.SubscriptionId == subscriptionId.Value);

    if (userId.HasValue) query = query.Where(e => e.UserId == userId.Value);

    return await query.OrderByDescending(e => e.CreatedAt).ToListAsync(cancellationToken);
  }

  public async Task<WebhookProcessingResult> RetryWebhookProcessingAsync(Guid webhookEventId, CancellationToken cancellationToken = default) {
    var webhookEvent = await _context.Set<BillingWebhookEvent>().FirstOrDefaultAsync(e => e.Id == webhookEventId, cancellationToken);

    if (webhookEvent == null) { return WebhookProcessingResult.Failure("Webhook event not found"); }

    if (webhookEvent.IsProcessed) { return WebhookProcessingResult.Failure("Webhook event already processed"); }

    var headers = string.IsNullOrEmpty(webhookEvent.Headers) ? new Dictionary<string, string>() : JsonSerializer.Deserialize<Dictionary<string, string>>(webhookEvent.Headers) ?? new Dictionary<string, string>();

    return await ProcessWebhookAsync(webhookEvent.Provider, webhookEvent.Payload, headers, cancellationToken);
  }

  private async Task<WebhookProcessingResult> ProcessStripeEvent(BillingWebhookEvent webhookEvent, CancellationToken cancellationToken) {
    try {
      return webhookEvent.EventType switch {
        "customer.subscription.created" => await HandleSubscriptionCreated(webhookEvent, cancellationToken),
        "customer.subscription.updated" => await HandleSubscriptionUpdated(webhookEvent, cancellationToken),
        "customer.subscription.deleted" => await HandleSubscriptionCancelled(webhookEvent, cancellationToken),
        "invoice.payment_succeeded" => await HandlePaymentSucceeded(webhookEvent, cancellationToken),
        "invoice.payment_failed" => await HandlePaymentFailed(webhookEvent, cancellationToken),
        _ => WebhookProcessingResult.Success() // Unsupported but not failed
      };
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error processing Stripe event {EventType}", webhookEvent.EventType);

      return WebhookProcessingResult.Failure($"Stripe event processing failed: {ex.Message}");
    }
  }

  private async Task<WebhookProcessingResult> ProcessPayPalEvent(BillingWebhookEvent webhookEvent, CancellationToken cancellationToken) {
    try {
      return webhookEvent.EventType switch {
        "BILLING.SUBSCRIPTION.CREATED" => await HandleSubscriptionCreated(webhookEvent, cancellationToken),
        "BILLING.SUBSCRIPTION.UPDATED" => await HandleSubscriptionUpdated(webhookEvent, cancellationToken),
        "BILLING.SUBSCRIPTION.CANCELLED" => await HandleSubscriptionCancelled(webhookEvent, cancellationToken),
        "PAYMENT.SALE.COMPLETED" => await HandlePaymentSucceeded(webhookEvent, cancellationToken),
        "PAYMENT.SALE.DENIED" => await HandlePaymentFailed(webhookEvent, cancellationToken),
        _ => WebhookProcessingResult.Success() // Unsupported but not failed
      };
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error processing PayPal event {EventType}", webhookEvent.EventType);

      return WebhookProcessingResult.Failure($"PayPal event processing failed: {ex.Message}");
    }
  }

  private async Task<WebhookProcessingResult> HandleSubscriptionCreated(BillingWebhookEvent webhookEvent, CancellationToken cancellationToken) {
    // TODO: Extract subscription data and create/update subscription
    _logger.LogInformation("Processing subscription created event from {Provider}", webhookEvent.Provider);

    return WebhookProcessingResult.Success();
  }

  private async Task<WebhookProcessingResult> HandleSubscriptionUpdated(BillingWebhookEvent webhookEvent, CancellationToken cancellationToken) {
    // TODO: Extract subscription data and update subscription
    _logger.LogInformation("Processing subscription updated event from {Provider}", webhookEvent.Provider);

    return WebhookProcessingResult.Success();
  }

  private async Task<WebhookProcessingResult> HandleSubscriptionCancelled(BillingWebhookEvent webhookEvent, CancellationToken cancellationToken) {
    // TODO: Extract subscription data and cancel subscription
    _logger.LogInformation("Processing subscription cancelled event from {Provider}", webhookEvent.Provider);

    return WebhookProcessingResult.Success();
  }

  private async Task<WebhookProcessingResult> HandlePaymentSucceeded(BillingWebhookEvent webhookEvent, CancellationToken cancellationToken) {
    // TODO: Extract payment data and create payment record
    _logger.LogInformation("Processing payment succeeded event from {Provider}", webhookEvent.Provider);

    return WebhookProcessingResult.Success();
  }

  private async Task<WebhookProcessingResult> HandlePaymentFailed(BillingWebhookEvent webhookEvent, CancellationToken cancellationToken) {
    // TODO: Extract payment data and handle failed payment
    _logger.LogInformation("Processing payment failed event from {Provider}", webhookEvent.Provider);

    return WebhookProcessingResult.Success();
  }

  private string? ExtractEventId(string payload, string provider) {
    try {
      var json = JsonSerializer.Deserialize<JsonElement>(payload);

      return provider.ToLowerInvariant() switch { "stripe" => json.TryGetProperty("id", out var stripeId) ? stripeId.GetString() : null, "paypal" => json.TryGetProperty("id", out var paypalId) ? paypalId.GetString() : null, _ => null };
    }
    catch { return null; }
  }

  private string? ExtractEventType(string payload, string provider) {
    try {
      var json = JsonSerializer.Deserialize<JsonElement>(payload);

      return provider.ToLowerInvariant() switch {
        "stripe" => json.TryGetProperty("type", out var stripeType) ? stripeType.GetString() : null, "paypal" => json.TryGetProperty("event_type", out var paypalType) ? paypalType.GetString() : null, _ => null
      };
    }
    catch { return null; }
  }
}
