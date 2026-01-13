using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Handler for retrying webhook events
/// </summary>
public class RetryWebhookEventHandler(ILogger<RetryWebhookEventHandler> logger) : ICommandHandler<RetryWebhookEventCommand, WebhookRetryResult>
{
    public async Task<WebhookRetryResult> Handle(RetryWebhookEventCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // TODO: Get webhook event from repository
            // var webhookEvent = await _webhookRepository.GetByIdAsync(Guid.Parse(command.EventId), cancellationToken);

            // For now, return mock result
            return await Task.FromResult(new WebhookRetryResult { Success = true, AttemptNumber = 2 });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrying webhook event {EventId}", command.EventId);

            return new WebhookRetryResult { Success = false, ErrorMessage = ex.Message, AttemptNumber = 1 };
        }
    }
}
