using GameGuild.CQRS;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Handler for ProcessPayPalWebhookCommand.
///     Delegates to PayPalBillingWebhookService for actual processing.
/// </summary>
public class ProcessPayPalWebhookCommandHandler(
    PayPalBillingWebhookService paypalWebhookService,
    ILogger<ProcessPayPalWebhookCommandHandler> logger
) : ICommandHandler<ProcessPayPalWebhookCommand, WebhookProcessingResult>
{
    public async Task<WebhookProcessingResult> Handle(ProcessPayPalWebhookCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (eventId, eventType) = ExtractEventInfo(request.Payload);
        logger.LogInformation(
            "Processing PayPal webhook: EventId={EventId}, EventType={EventType}, PayloadLength={Length}",
            eventId, eventType, request.Payload.Length);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var result = await paypalWebhookService.ProcessPayPalWebhookAsync(
                request.WebhookId ?? string.Empty,
                request.Payload,
                request.TransmissionId,
                request.TransmissionTime,
                request.TransmissionSignature,
                request.CertUrl,
                request.AuthAlgo,
                cancellationToken).ConfigureAwait(false);

            stopwatch.Stop();
            LogWebhookMetrics(eventId, eventType, result, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex,
                "PayPal webhook processing failed: EventId={EventId}, ElapsedMs={ElapsedMs}",
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
            
            var eventId = root.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "unknown" : "unknown";
            var eventType = root.TryGetProperty("event_type", out var typeProp) ? typeProp.GetString() ?? "unknown" : "unknown";
            
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
                "PayPal webhook processed successfully: EventId={EventId}, EventType={EventType}, ElapsedMs={ElapsedMs}",
                eventId, eventType, elapsedMs);
        }
        else if (result.WasAlreadyProcessed)
        {
            logger.LogDebug(
                "PayPal webhook already processed: EventId={EventId}, EventType={EventType}",
                eventId, eventType);
        }
        else
        {
            logger.LogWarning(
                "PayPal webhook processing failed: EventId={EventId}, EventType={EventType}, Error={Error}, ElapsedMs={ElapsedMs}",
                eventId, eventType, result.ErrorMessage, elapsedMs);
        }
    }
}
