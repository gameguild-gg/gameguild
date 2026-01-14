using GameGuild.CQRS;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Handler for ProcessApplePayWebhookCommand.
///     Delegates to ApplePayBillingWebhookService for actual processing.
/// </summary>
public class ProcessApplePayWebhookCommandHandler(
    ApplePayBillingWebhookService applePayWebhookService,
    ILogger<ProcessApplePayWebhookCommandHandler> logger
) : ICommandHandler<ProcessApplePayWebhookCommand, WebhookProcessingResult>
{
    public async Task<WebhookProcessingResult> Handle(ProcessApplePayWebhookCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (eventId, eventType) = ExtractEventInfo(request.Payload);
        logger.LogInformation(
            "Processing Apple Pay webhook: EventId={EventId}, EventType={EventType}, PayloadLength={Length}",
            eventId, eventType, request.Payload.Length);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Use ProcessAppStoreNotificationAsync for App Store Server Notifications V2
            // The Payload is the signed JWS from Apple, which contains the full notification
            var result = await applePayWebhookService.ProcessAppStoreNotificationAsync(
                request.Payload,
                cancellationToken).ConfigureAwait(false);

            stopwatch.Stop();
            LogWebhookMetrics(eventId, eventType, result, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex,
                "Apple Pay webhook processing failed: EventId={EventId}, ElapsedMs={ElapsedMs}",
                eventId, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private static (string eventId, string eventType) ExtractEventInfo(string payload)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;
            
            var eventId = root.TryGetProperty("eventId", out var idProp) 
                ? idProp.GetString() ?? "unknown" 
                : (root.TryGetProperty("transactionId", out var txProp) ? txProp.GetString() ?? "unknown" : "unknown");
            var eventType = root.TryGetProperty("eventType", out var typeProp) ? typeProp.GetString() ?? "unknown" : "unknown";
            
            return (eventId, eventType);
        }
        catch
        {
            return ("unknown", "unknown");
        }
    }

    private void LogWebhookMetrics(string eventId, string eventType, WebhookProcessingResult result, long elapsedMs)
    {
        if (result.Processed)
        {
            logger.LogInformation(
                "Apple Pay webhook processed successfully: EventId={EventId}, EventType={EventType}, ElapsedMs={ElapsedMs}",
                eventId, eventType, elapsedMs);
        }
        else if (result.WasAlreadyProcessed)
        {
            logger.LogDebug(
                "Apple Pay webhook already processed: EventId={EventId}, EventType={EventType}",
                eventId, eventType);
        }
        else
        {
            logger.LogWarning(
                "Apple Pay webhook processing failed: EventId={EventId}, EventType={EventType}, Error={Error}, ElapsedMs={ElapsedMs}",
                eventId, eventType, result.ErrorMessage, elapsedMs);
        }
    }
}
