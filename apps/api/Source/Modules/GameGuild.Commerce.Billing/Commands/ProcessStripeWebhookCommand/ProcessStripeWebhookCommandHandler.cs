using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

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

        var result = await stripeWebhookService.ProcessStripeWebhookAsync(
                request.Payload,
                request.Signature,
                cancellationToken
            ).ConfigureAwait(false);

        LogWebhookMetrics("stripe", result);

        return result;
    }

    /// <summary>
    ///     Logs webhook processing metrics for observability.
    /// </summary>
    private void LogWebhookMetrics(string provider, WebhookProcessingResult result)
    {
        if (result.Processed)
        {
            logger.LogInformation(
                "[WEBHOOK_METRIC] Provider={Provider} Status=Success EventId={EventId} WasAlreadyProcessed={WasAlreadyProcessed}",
                provider, result.EventId, result.WasAlreadyProcessed);
        }
        else
        {
            logger.LogWarning(
                "[WEBHOOK_METRIC] Provider={Provider} Status=Failed EventId={EventId} Error={Error} RequiresRetry={RequiresRetry}",
                provider, result.EventId, result.ErrorMessage, result.RequiresRetry);
        }
    }
}
