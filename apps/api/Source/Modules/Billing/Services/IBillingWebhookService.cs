using GameGuild.Modules.Billing.Models;


namespace GameGuild.Modules.Billing.Services;

/// <summary> Service for handling billing webhooks from various providers </summary>
public interface IBillingWebhookService {
  /// <summary> Process a generic billing webhook </summary>
  Task<WebhookProcessingResult> ProcessWebhookAsync(string provider, string payload, Dictionary<string, string> headers, CancellationToken cancellationToken = default);

  /// <summary> Process a Stripe webhook </summary>
  Task<WebhookProcessingResult> ProcessStripeWebhookAsync(string payload, string signatureHeader, CancellationToken cancellationToken = default);

  /// <summary> Process a PayPal webhook </summary>
  Task<WebhookProcessingResult> ProcessPayPalWebhookAsync(string payload, Dictionary<string, string> headers, CancellationToken cancellationToken = default);

  /// <summary> Get webhook event by external ID </summary>
  Task<BillingWebhookEvent?> GetWebhookEventAsync(string provider, string externalEventId, CancellationToken cancellationToken = default);

  /// <summary> Get webhook events for a specific entity </summary>
  Task<IEnumerable<BillingWebhookEvent>> GetWebhookEventsAsync(Guid? tenantId = null, Guid? subscriptionId = null, Guid? userId = null, CancellationToken cancellationToken = default);

  /// <summary> Retry processing a failed webhook </summary>
  Task<WebhookProcessingResult> RetryWebhookProcessingAsync(Guid webhookEventId, CancellationToken cancellationToken = default);
}
