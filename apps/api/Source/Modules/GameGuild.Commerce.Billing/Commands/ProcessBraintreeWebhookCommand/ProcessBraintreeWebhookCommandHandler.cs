using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Handler for ProcessBraintreeWebhookCommand.
///     Delegates to BraintreeBillingWebhookService for actual processing.
/// </summary>
public sealed class ProcessBraintreeWebhookCommandHandler(
    BraintreeBillingWebhookService braintreeWebhookService,
    ILogger<ProcessBraintreeWebhookCommandHandler> logger)
    : ICommandHandler<ProcessBraintreeWebhookCommand, WebhookProcessingResult>
{
    public async Task<WebhookProcessingResult> Handle(ProcessBraintreeWebhookCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        logger.LogInformation(
            "Processing Braintree webhook: SignatureLength={SignatureLength}, PayloadLength={PayloadLength}",
            request.BtSignature.Length,
            request.BtPayload.Length);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var result = await braintreeWebhookService.ProcessBraintreeWebhookAsync(
                    request.BtSignature,
                    request.BtPayload,
                    cancellationToken)
                .ConfigureAwait(false);

            stopwatch.Stop();
            LogWebhookMetrics(result, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex,
                "Braintree webhook processing failed: ElapsedMs={ElapsedMs}",
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private void LogWebhookMetrics(WebhookProcessingResult result, long elapsedMs)
    {
        if (result.Processed && !result.WasAlreadyProcessed)
        {
            logger.LogInformation(
                "Braintree webhook processed successfully: EventId={EventId}, ElapsedMs={ElapsedMs}",
                result.EventId,
                elapsedMs);
        }
        else if (result.WasAlreadyProcessed)
        {
            logger.LogDebug("Braintree webhook already processed: EventId={EventId}", result.EventId);
        }
        else
        {
            logger.LogWarning(
                "Braintree webhook processing failed: EventId={EventId}, Error={Error}, ElapsedMs={ElapsedMs}",
                result.EventId,
                result.ErrorMessage,
                elapsedMs);
        }
    }
}
