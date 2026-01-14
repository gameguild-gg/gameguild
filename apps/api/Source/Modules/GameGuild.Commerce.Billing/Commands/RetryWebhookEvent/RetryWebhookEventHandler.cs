using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Handler for retrying webhook events
/// </summary>
public class RetryWebhookEventHandler(
    IBillingWebhookRepository webhookRepository,
    ILogger<RetryWebhookEventHandler> logger) : ICommandHandler<RetryWebhookEventCommand, WebhookRetryResult>
{
    private const int MaxRetryAttempts = 5;

    public async Task<WebhookRetryResult> Handle(RetryWebhookEventCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (!Guid.TryParse(command.EventId, out var eventId))
            {
                logger.LogWarning("Invalid event ID format for retry: {EventId}", command.EventId);
                return new WebhookRetryResult
                {
                    Success = false,
                    ErrorMessage = "Invalid event ID format",
                    AttemptNumber = 0
                };
            }

            var webhookEvent = await webhookRepository.GetByIdAsync(eventId, cancellationToken)
                .ConfigureAwait(false);

            if (webhookEvent == null)
            {
                logger.LogWarning("Webhook event not found for retry: {EventId}", command.EventId);
                return new WebhookRetryResult
                {
                    Success = false,
                    ErrorMessage = "Webhook event not found",
                    AttemptNumber = 0
                };
            }

            // Check if already processed successfully
            if (webhookEvent.IsProcessed && !webhookEvent.IsFailed)
            {
                logger.LogInformation("Webhook event {EventId} already processed successfully", command.EventId);
                return new WebhookRetryResult
                {
                    Success = true,
                    AttemptNumber = webhookEvent.ProcessingAttempts,
                    Message = "Event was already processed successfully"
                };
            }

            // Check max retry attempts
            if (webhookEvent.ProcessingAttempts >= MaxRetryAttempts)
            {
                logger.LogWarning("Webhook event {EventId} has exceeded max retry attempts ({MaxAttempts})",
                    command.EventId, MaxRetryAttempts);
                return new WebhookRetryResult
                {
                    Success = false,
                    ErrorMessage = $"Maximum retry attempts ({MaxRetryAttempts}) exceeded",
                    AttemptNumber = webhookEvent.ProcessingAttempts
                };
            }

            // Increment attempts and reset failed state for retry
            webhookEvent.IncrementAttempts();
            webhookEvent.IsFailed = false;
            webhookEvent.ErrorMessage = null;

            await webhookRepository.UpdateAsync(webhookEvent, cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Webhook event {EventId} marked for retry. Attempt {Attempt} of {MaxAttempts}",
                command.EventId, webhookEvent.ProcessingAttempts, MaxRetryAttempts);

            // The actual reprocessing would be done by a background job or 
            // the caller can trigger reprocessing based on the provider
            return new WebhookRetryResult
            {
                Success = true,
                AttemptNumber = webhookEvent.ProcessingAttempts,
                Message = $"Event queued for retry (attempt {webhookEvent.ProcessingAttempts})"
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrying webhook event {EventId}", command.EventId);
            return new WebhookRetryResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                AttemptNumber = 0
            };
        }
    }
}
