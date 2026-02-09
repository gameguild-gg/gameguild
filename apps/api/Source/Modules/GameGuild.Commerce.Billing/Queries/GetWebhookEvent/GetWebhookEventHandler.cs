using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Handler for getting webhook events
/// </summary>
public sealed class GetWebhookEventHandler(
    IBillingWebhookRepository webhookRepository,
    ILogger<GetWebhookEventHandler> logger) : IQueryHandler<GetWebhookEventQuery, BillingWebhookEventDto?>
{
    public async Task<BillingWebhookEventDto?> Handle(GetWebhookEventQuery query, CancellationToken cancellationToken)
    {
        try
        {
            if (!Guid.TryParse(query.EventId, out var eventId))
            {
                logger.LogWarning("Invalid event ID format: {EventId}", query.EventId);
                return null;
            }

            var webhookEvent = await webhookRepository.GetByIdAsync(eventId, cancellationToken)
                .ConfigureAwait(false);

            if (webhookEvent == null)
            {
                logger.LogDebug("Webhook event not found: {EventId}", query.EventId);
                return null;
            }

            return new BillingWebhookEventDto
            {
                Id = webhookEvent.Id,
                Provider = webhookEvent.Provider,
                ExternalEventId = webhookEvent.ExternalEventId,
                EventType = webhookEvent.EventType,
                IsProcessed = webhookEvent.IsProcessed,
                IsFailed = webhookEvent.IsFailed,
                ProcessingAttempts = webhookEvent.ProcessingAttempts,
                ErrorMessage = webhookEvent.ErrorMessage,
                ProcessedAt = webhookEvent.ProcessedAt,
                CreatedAt = webhookEvent.CreatedAt,
                TenantId = webhookEvent.TenantId,
                SubscriptionId = webhookEvent.SubscriptionId
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting webhook event {EventId}", query.EventId);
            throw;
        }
    }
}
