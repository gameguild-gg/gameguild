using Microsoft.Extensions.Logging;
using GameGuild.Modules.Billing.Models;
using MediatR;

namespace GameGuild.Modules.Billing.Features.ManageWebhook;

/// <summary>
///     Handler for retrying webhook events
/// </summary>
public class RetryWebhookEventHandler : ICommandHandler<RetryWebhookEventCommand, WebhookRetryResult>
{
    private readonly ILogger<RetryWebhookEventHandler> _logger;

    public RetryWebhookEventHandler(ILogger<RetryWebhookEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task<WebhookRetryResult> Handle(RetryWebhookEventCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // TODO: Get webhook event from repository
            // var webhookEvent = await _webhookRepository.GetByIdAsync(Guid.Parse(command.EventId), cancellationToken);

            // For now, return mock result
            return await Task.FromResult(new WebhookRetryResult
            {
                Success = true,
                AttemptNumber = 2
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying webhook event {EventId}", command.EventId);

            return new WebhookRetryResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                AttemptNumber = 1
            };
        }
    }
}

