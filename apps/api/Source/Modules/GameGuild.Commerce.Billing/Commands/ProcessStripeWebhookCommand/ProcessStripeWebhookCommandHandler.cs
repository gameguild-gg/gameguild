using GameGuild.CQRS;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Handler for ProcessStripeWebhookCommand.
///     Integrates with StripeBillingWebhookService for full webhook processing.
/// </summary>
public sealed class ProcessStripeWebhookCommandHandler(
    StripeBillingWebhookService stripeWebhookService,
    ILogger<ProcessStripeWebhookCommandHandler> logger
) : ICommandHandler<ProcessStripeWebhookCommand, WebhookProcessingResult>
{
    public async Task<WebhookProcessingResult> Handle(ProcessStripeWebhookCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        logger.LogInformation(
            "Processing Stripe webhook. Payload length: {PayloadLength}, Has signature: {HasSignature}",
            request.Payload.Length,
            !string.IsNullOrEmpty(request.Signature));

        try
        {
            // Extract event ID and type from payload
            var (eventId, eventType) = ExtractEventInfo(request.Payload);
            
            if (string.IsNullOrEmpty(eventId))
            {
                logger.LogWarning("Stripe webhook missing event ID");
                return WebhookProcessingResult.Failed("unknown", "Missing event ID in payload");
            }

            // Process via the dedicated Stripe webhook service
            var result = await stripeWebhookService.ProcessStripeWebhookAsync(
                eventId,
                eventType,
                request.Payload,
                request.Signature,
                cancellationToken
            );

            // Log metrics for monitoring
            LogWebhookMetrics("stripe", eventType, result);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error processing Stripe webhook");
            return WebhookProcessingResult.Failed("unknown", ex.Message);
        }
    }

    /// <summary>
    ///     Extracts event ID and type from Stripe webhook payload.
    /// </summary>
    private static (string eventId, string eventType) ExtractEventInfo(string payload)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;
            
            var eventId = root.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
            var eventType = root.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;

            return (eventId ?? string.Empty, eventType ?? "unknown");
        }
        catch
        {
            return (string.Empty, "unknown");
        }
    }

    /// <summary>
    ///     Logs webhook processing metrics for observability.
    /// </summary>
    private void LogWebhookMetrics(string provider, string eventType, WebhookProcessingResult result)
    {
        if (result.Processed)
        {
            logger.LogInformation(
                "[WEBHOOK_METRIC] Provider={Provider} EventType={EventType} Status=Success EventId={EventId} WasAlreadyProcessed={WasAlreadyProcessed}",
                provider, eventType, result.EventId, result.WasAlreadyProcessed);
        }
        else
        {
            logger.LogWarning(
                "[WEBHOOK_METRIC] Provider={Provider} EventType={EventType} Status=Failed EventId={EventId} Error={Error} RequiresRetry={RequiresRetry}",
                provider, eventType, result.EventId, result.ErrorMessage, result.RequiresRetry);
        }
    }
}
