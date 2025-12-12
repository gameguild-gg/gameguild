using GameGuild.Billing.DTOs;
using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Billing.Queries;

/// <summary>
///     Handler for getting webhook events
/// </summary>
public class GetWebhookEventHandler(ILogger<GetWebhookEventHandler> logger) : IQueryHandler<GetWebhookEventQuery, BillingWebhookEventDto?>
{
    public async Task<BillingWebhookEventDto?> Handle(GetWebhookEventQuery query, CancellationToken cancellationToken)
    {
        try
        {
            // TODO: Get webhook event from repository
            // var webhookEvent = await _webhookRepository.GetByIdAsync(Guid.Parse(query.EventId), cancellationToken);

            // For now, return mock result
            return await Task.FromResult(
                new BillingWebhookEventDto
                {
                    Id = Guid.Parse(query.EventId), Provider = "stripe", ExternalEventId = "evt_test_12345", EventType = "payment_intent.succeeded", IsProcessed = true, CreatedAt = DateTime.UtcNow
                }
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting webhook event {EventId}", query.EventId);

            return null;
        }
    }
}
