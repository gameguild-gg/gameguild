using GameGuild.Compliance.Audit;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Outcome of a webhook row as journaled to the audit module.
/// </summary>
public enum WebhookLifecycleOutcome
{
    /// <summary>The delivery was accepted into the durable inbox.</summary>
    Received = 0,

    /// <summary>The inbox row completed processing.</summary>
    Processed = 1,

    /// <summary>The inbox row recorded a failed processing attempt.</summary>
    Failed = 2
}

/// <summary>
///     Journals the lifecycle of every billing webhook inbox row into the Compliance.Audit
///     pipeline (issue #408: an audit-module entry for every webhook event). Publishing is
///     best-effort from the persistence path: an audit failure is logged, never propagated —
///     the durable inbox row remains the source of truth for delivery state either way.
/// </summary>
public interface IWebhookLifecycleAuditPublisher
{
    /// <summary>Journals one lifecycle transition of a webhook inbox row.</summary>
    /// <param name="outcome">The transition being journaled.</param>
    /// <param name="webhookEvent">The row the transition applies to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync(
        WebhookLifecycleOutcome outcome,
        BillingWebhookEvent webhookEvent,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     Default publisher: records through <see cref="ISecurityEventLogger.RecordAsync"/> —
///     the single pipeline entry point of the audit module — using the webhook lifecycle
///     action types. Lifecycle entries are operational (System category, Low/Medium risk):
///     the pipeline classifies them as non-security-relevant and journals them through the
///     standard audit log, while security-relevant webhook events keep their dedicated
///     threat action types.
/// </summary>
public sealed class WebhookLifecycleAuditPublisher(
    ISecurityEventLogger securityEventLogger,
    ILogger<WebhookLifecycleAuditPublisher> logger) : IWebhookLifecycleAuditPublisher
{
    private const int MaxDetailLength = 500;

    public async Task PublishAsync(
        WebhookLifecycleOutcome outcome,
        BillingWebhookEvent webhookEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(webhookEvent);

        try
        {
            var actionType = outcome switch
            {
                WebhookLifecycleOutcome.Received => AuditActionTypes.WebhookEventReceived,
                WebhookLifecycleOutcome.Processed => AuditActionTypes.WebhookEventProcessed,
                _ => AuditActionTypes.WebhookEventFailed
            };

            var request = new CreateAuditLogRequest
            {
                ActionType = actionType,
                ResourceType = "BillingWebhook",
                ResourceId = webhookEvent.Provider,
                IpAddress = webhookEvent.SourceIpAddress,
                Category = AuditCategory.System,
                RiskLevel = outcome == WebhookLifecycleOutcome.Failed ? AuditRiskLevel.Medium : AuditRiskLevel.Low,
                Success = outcome != WebhookLifecycleOutcome.Failed,
                ErrorMessage = outcome == WebhookLifecycleOutcome.Failed ? Bound(webhookEvent.ErrorMessage) : null,
                Description = $"Billing webhook {outcome.ToString().ToLowerInvariant()} ({webhookEvent.Provider}/{webhookEvent.EventType}).",
                Metadata = new Dictionary<string, object?>
                {
                    ["provider"] = webhookEvent.Provider,
                    ["externalEventId"] = webhookEvent.ExternalEventId,
                    ["inboxRowId"] = webhookEvent.Id,
                    ["eventType"] = webhookEvent.EventType,
                    ["processingAttempts"] = webhookEvent.ProcessingAttempts,
                    ["deadLettered"] = webhookEvent.DeadLetteredAt != null
                }
            };

            await securityEventLogger.RecordAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Best effort: audit journaling must never change the webhook persistence outcome.
            logger.LogError(
                exception,
                "Failed to journal webhook lifecycle {Outcome} for provider {Provider} event {EventId}",
                outcome,
                webhookEvent.Provider,
                webhookEvent.ExternalEventId);
        }
    }

    private static string? Bound(string? value) =>
        string.IsNullOrEmpty(value) || value.Length <= MaxDetailLength ? value : value[..MaxDetailLength];
}
