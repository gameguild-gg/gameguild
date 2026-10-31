using GameGuild.Commerce.Subscriptions;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Braintree-specific implementation of the billing webhook service (issue #408: the
///     verification the Commerce.Payments adapter deferred to this ingress). The delivery is
///     verified BEFORE any inbox work: the offline bt_signature check authenticates the
///     payload, and only then is a deterministic event id derived from the decoded
///     notification (kind + subject + timestamp) for idempotency. The stored payload is the
///     privacy-minimized JSON projection of the notification, never the raw provider body.
/// </summary>
public class BraintreeBillingWebhookService : BillingWebhookService, IBillingWebhookReplayHandler
{
    /// <summary>Replay-handler provider key (issue #408 execution arm).</summary>
    string IBillingWebhookReplayHandler.Provider => ProviderName;

    Task<WebhookProcessingResult> IBillingWebhookReplayHandler.ReplayAsync(
        BillingWebhookEvent webhookEvent,
        CancellationToken cancellationToken) =>
        ReplayBraintreeWebhookAsync(webhookEvent, cancellationToken);

    private readonly IBillingWebhookRepository _webhookRepository;
    private readonly IBraintreeWebhookVerificationService _verificationService;
    private readonly IWebhookSecurityEventPublisher? _securityEvents;
    private readonly IBillingIntegrationEventPublisher? _billingEvents;
    private readonly ILogger<BraintreeBillingWebhookService> _logger;

    /// <inheritdoc />
    protected override string ProviderName => PaymentProviders.Braintree;

    public BraintreeBillingWebhookService(
        IBillingWebhookRepository webhookRepository,
        IBraintreeWebhookVerificationService verificationService,
        ILogger<BraintreeBillingWebhookService> logger,
        ISubscriptionLifecycleService lifecycleService,
        ISubscriptionQueryService queryService,
        ISubscriptionBillingService billingService,
        ISubscriptionExternalIdService externalIdService,
        IWebhookSecurityEventPublisher? securityEvents = null,
        IBillingIntegrationEventPublisher? billingEvents = null)
        : base(logger, lifecycleService, queryService, billingService, externalIdService, billingEvents)
    {
        _webhookRepository = webhookRepository;
        _verificationService = verificationService;
        _securityEvents = securityEvents;
        _billingEvents = billingEvents;
        _logger = logger;
    }

    /// <summary>
    ///     Processes a Braintree webhook delivery with offline signature verification and an
    ///     idempotent inbox (issue #408).
    /// </summary>
    /// <param name="btSignature">Raw <c>bt_signature</c> form field.</param>
    /// <param name="btPayload">Raw <c>bt_payload</c> form field (base64).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Processing result.</returns>
    public async Task<WebhookProcessingResult> ProcessBraintreeWebhookAsync(
        string btSignature,
        string btPayload,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing Braintree webhook delivery");

        // Verify FIRST: every downstream decision (idempotency key, routing, persistence)
        // operates only on authenticated provider data. A failed verification is recorded
        // as a security event and rejected without an inbox row — unlike the header-keyed
        // providers there is no trustworthy identity before the signature check.
        var verification = _verificationService.Verify(btSignature, btPayload);
        if (!verification.IsValid)
        {
            _logger.LogWarning("Braintree webhook signature verification failed: {Error}", verification.ErrorMessage);
            await PublishSecurityEventAsync(
                    WebhookSecurityEventKind.SignatureFailed,
                    verification.ErrorMessage ?? "Invalid webhook signature",
                    eventId: null,
                    cancellationToken)
                .ConfigureAwait(false);
            return WebhookProcessingResult.Failed("unknown", verification.ErrorMessage ?? "Invalid webhook signature", requiresRetry: false);
        }

        var notification = verification.Notification!;
        var eventId = DeriveEventId(notification);

        // Idempotency (issue #408): only a delivery that previously COMPLETED is acknowledged
        // as a duplicate; pending or failed rows are re-processed instead of masked.
        var existingEvent = await _webhookRepository
            .GetByExternalEventIdAsync(eventId, PaymentProviders.Braintree, cancellationToken)
            .ConfigureAwait(false);
        if (existingEvent?.IsProcessed == true)
        {
            _logger.LogInformation("Duplicate Braintree webhook detected: {EventId}. Returning success.", eventId);
            await PublishSecurityEventAsync(
                    WebhookSecurityEventKind.ReplayDetected,
                    "Duplicate Braintree webhook delivery acknowledged by the idempotent inbox.",
                    eventId,
                    cancellationToken)
                .ConfigureAwait(false);
            return WebhookProcessingResult.AlreadyProcessed(eventId, existingEvent.ProcessedAt);
        }

        var minimizedPayload = MinimizePayload(notification);

        var webhookEvent = existingEvent ?? new BillingWebhookEvent
        {
            ExternalEventId = eventId,
            Provider = PaymentProviders.Braintree,
            EventType = notification.Kind,
            Payload = minimizedPayload,
            ProcessingAttempts = 1
        };

        try
        {
            if (existingEvent is null)
            {
                webhookEvent = await _webhookRepository.CreateAsync(webhookEvent, cancellationToken).ConfigureAwait(false);

                if (webhookEvent.IsProcessed)
                {
                    return WebhookProcessingResult.AlreadyProcessed(webhookEvent.ExternalEventId, webhookEvent.ProcessedAt);
                }
            }
            else
            {
                webhookEvent.IncrementAttempts();
                webhookEvent.IsFailed = false;
                webhookEvent.ErrorMessage = null;
            }

            // The delivery was authenticated before the row existed; every replay of this
            // row is therefore authorized to re-route (issue #408).
            webhookEvent.MarkSignatureVerified();

            await RouteBraintreeEventAsync(notification, cancellationToken).ConfigureAwait(false);

            webhookEvent.MarkAsProcessed();
            await _webhookRepository.UpdateAsync(webhookEvent, cancellationToken).ConfigureAwait(false);
            await PublishBillingEventAsync(webhookEvent, processed: true, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully processed Braintree webhook: {EventId} ({Kind})", eventId, notification.Kind);
            return WebhookProcessingResult.Success(eventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Braintree webhook: {EventId}", eventId);

            webhookEvent.MarkAsFailed(ex.Message);
            await _webhookRepository.UpdateAsync(webhookEvent, cancellationToken).ConfigureAwait(false);
            await PublishBillingEventAsync(webhookEvent, processed: false, cancellationToken).ConfigureAwait(false);

            return WebhookProcessingResult.Failed(eventId, ex.Message);
        }
    }

    /// <summary>
    ///     Replays a previously accepted Braintree inbox row through its event routing
    ///     (issue #408). Fail closed: only rows whose delivery was signature-verified are
    ///     re-routed; Braintree rows are always verified before acceptance, so legacy
    ///     unverified rows (from other import paths) are refused.
    /// </summary>
    public async Task<WebhookProcessingResult> ReplayBraintreeWebhookAsync(
        BillingWebhookEvent webhookEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(webhookEvent);

        if (!string.Equals(webhookEvent.Provider, PaymentProviders.Braintree, StringComparison.Ordinal))
        {
            throw new ArgumentException("The webhook event does not belong to the Braintree provider.", nameof(webhookEvent));
        }

        if (webhookEvent.IsProcessed)
        {
            return WebhookProcessingResult.AlreadyProcessed(webhookEvent.ExternalEventId, webhookEvent.ProcessedAt);
        }

        if (webhookEvent.IsSignatureVerified != true)
        {
            const string reason = "Replay refused: the stored delivery was not signature-verified.";
            _logger.LogWarning("Braintree webhook replay refused for {EventId}: not signature-verified.", webhookEvent.ExternalEventId);
            webhookEvent.MarkAsFailed(reason);
            await _webhookRepository.UpdateAsync(webhookEvent, cancellationToken).ConfigureAwait(false);
            return WebhookProcessingResult.Failed(webhookEvent.ExternalEventId, reason, requiresRetry: false);
        }

        try
        {
            var notification = ParseMinimizedPayload(webhookEvent.Payload);
            if (notification is null)
            {
                const string reason = "Replay refused: the stored payload projection could not be parsed.";
                webhookEvent.MarkAsFailed(reason);
                await _webhookRepository.UpdateAsync(webhookEvent, cancellationToken).ConfigureAwait(false);
                return WebhookProcessingResult.Failed(webhookEvent.ExternalEventId, reason, requiresRetry: false);
            }

            await RouteBraintreeEventAsync(notification, cancellationToken).ConfigureAwait(false);

            webhookEvent.MarkAsProcessed();
            await _webhookRepository.UpdateAsync(webhookEvent, cancellationToken).ConfigureAwait(false);
            await PublishBillingEventAsync(webhookEvent, processed: true, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Successfully replayed Braintree webhook: {EventId} ({Kind})",
                webhookEvent.ExternalEventId,
                webhookEvent.EventType);
            return WebhookProcessingResult.Success(webhookEvent.ExternalEventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to replay Braintree webhook: {EventId}", webhookEvent.ExternalEventId);

            webhookEvent.MarkAsFailed(ex.Message);
            await _webhookRepository.UpdateAsync(webhookEvent, cancellationToken).ConfigureAwait(false);
            await PublishBillingEventAsync(webhookEvent, processed: false, cancellationToken).ConfigureAwait(false);

            return WebhookProcessingResult.Failed(webhookEvent.ExternalEventId, ex.Message);
        }
    }

    /// <summary>
    ///     Deterministic idempotency identity of a verified notification: kind, subject id
    ///     and provider timestamp are stable across provider redeliveries of the same event.
    /// </summary>
    internal static string DeriveEventId(BraintreeWebhookNotification notification)
    {
        var subject = notification.SubscriptionId
                      ?? notification.TransactionId
                      ?? notification.DisputeId
                      ?? "notification";
        var timestamp = notification.TimestampUtc?.ToString("yyyyMMddHHmmss") ?? "untimed";
        return $"braintree-{notification.Kind}-{subject}-{timestamp}";
    }

    /// <summary>
    ///     Privacy-minimized JSON projection of the notification persisted on the inbox row:
    ///     routing identifiers only, never the raw provider body (which may carry customer
    ///     PII under different subject kinds).
    /// </summary>
    internal static string MinimizePayload(BraintreeWebhookNotification notification) =>
        JsonSerializer.Serialize(new
        {
            kind = notification.Kind,
            timestampUtc = notification.TimestampUtc,
            subscriptionId = notification.SubscriptionId,
            subscriptionStatus = notification.SubscriptionStatus,
            transactionId = notification.TransactionId,
            transactionStatus = notification.TransactionStatus,
            amount = notification.Amount,
            currencyIsoCode = notification.CurrencyIsoCode,
            disputeId = notification.DisputeId,
            disputeStatus = notification.DisputeStatus
        });

    /// <summary>Restores the routing projection persisted by <see cref="MinimizePayload"/>.</summary>
    internal static BraintreeWebhookNotification? ParseMinimizedPayload(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            DateTime? ReadTimestamp()
            {
                return root.TryGetProperty("timestampUtc", out var value)
                       && value.ValueKind == JsonValueKind.String
                       && DateTime.TryParse(value.GetString(), out var parsed)
                    ? parsed
                    : null;
            }

            return new BraintreeWebhookNotification
            {
                Kind = ReadString(root, "kind") ?? string.Empty,
                TimestampUtc = ReadTimestamp(),
                SubscriptionId = ReadString(root, "subscriptionId"),
                SubscriptionStatus = ReadString(root, "subscriptionStatus"),
                TransactionId = ReadString(root, "transactionId"),
                TransactionStatus = ReadString(root, "transactionStatus"),
                Amount = root.TryGetProperty("amount", out var amount) && amount.ValueKind == JsonValueKind.Number && amount.TryGetDecimal(out var parsedAmount)
                    ? parsedAmount
                    : null,
                CurrencyIsoCode = ReadString(root, "currencyIsoCode"),
                DisputeId = ReadString(root, "disputeId"),
                DisputeStatus = ReadString(root, "disputeStatus")
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task RouteBraintreeEventAsync(
        BraintreeWebhookNotification notification,
        CancellationToken cancellationToken)
    {
        switch (notification.Kind)
        {
            case "subscription_charged_successfully":
                if (notification.SubscriptionId is { } chargedSubscription)
                {
                    await HandlePaymentSucceededAsync(ToPaymentPayload(notification, chargedSubscription)).ConfigureAwait(false);
                }
                break;

            case "subscription_charged_unsuccessfully":
                if (notification.SubscriptionId is { } failedSubscription)
                {
                    await HandlePaymentFailedAsync(ToPaymentPayload(notification, failedSubscription)).ConfigureAwait(false);
                }
                break;

            case "subscription_went_active":
            case "subscription_went_past_due":
            case "subscription_trial_went_active":
                if (notification.SubscriptionId is { } updatedSubscription)
                {
                    await HandleSubscriptionUpdatedAsync(ToSubscriptionPayload(notification, updatedSubscription)).ConfigureAwait(false);
                }
                break;

            case "subscription_canceled":
            case "subscription_expired":
                if (notification.SubscriptionId is { } canceledSubscription)
                {
                    await HandleSubscriptionCanceledAsync(ToSubscriptionPayload(notification, canceledSubscription)).ConfigureAwait(false);
                }
                break;

            default:
                // Graceful unknown-type handling (issue #408): disputes and account-level
                // kinds are acknowledged and persisted for audit without failing delivery.
                _logger.LogDebug("Unhandled Braintree event kind: {Kind}", notification.Kind);
                break;
        }
    }

    /// <summary>Projects a verified notification onto the subscription routing payload.</summary>
    private static BraintreeSubscriptionWebhookPayload ToSubscriptionPayload(
        BraintreeWebhookNotification notification,
        string subscriptionId) => new()
    {
        ExternalSubscriptionId = subscriptionId,
        Status = notification.SubscriptionStatus ?? notification.Kind,
        Amount = notification.Amount ?? 0m,
        StartDate = notification.TimestampUtc,
        BraintreeStatus = notification.SubscriptionStatus
    };

    /// <summary>Projects a verified notification onto the payment routing payload.</summary>
    private static BraintreePaymentWebhookPayload ToPaymentPayload(
        BraintreeWebhookNotification notification,
        string subscriptionId) => new()
    {
        PaymentId = notification.TransactionId ?? subscriptionId,
        ExternalSubscriptionId = subscriptionId,
        Amount = notification.Amount ?? 0m,
        Currency = string.IsNullOrWhiteSpace(notification.CurrencyIsoCode) ? "USD" : notification.CurrencyIsoCode,
        Status = notification.TransactionStatus ?? notification.Kind,
        PaidAt = notification.TimestampUtc ?? SystemClock.UtcNow,
        FailureReason = notification.Kind == "subscription_charged_unsuccessfully" ? notification.TransactionStatus : null,
        TransactionId = notification.TransactionId,
        TransactionStatus = notification.TransactionStatus
    };

    private async Task PublishBillingEventAsync(
        BillingWebhookEvent webhookEvent,
        bool processed,
        CancellationToken cancellationToken)
    {
        if (_billingEvents is null)
        {
            return;
        }

        if (processed)
        {
            await _billingEvents.PublishWebhookProcessedAsync(webhookEvent, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _billingEvents.PublishWebhookFailedAsync(webhookEvent, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task PublishSecurityEventAsync(
        WebhookSecurityEventKind kind,
        string detail,
        string? eventId,
        CancellationToken cancellationToken)
    {
        if (_securityEvents is null)
        {
            return;
        }

        await _securityEvents.PublishAsync(
                kind,
                PaymentProviders.Braintree,
                sourceIpAddress: null,
                detail,
                eventId,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static string? ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
