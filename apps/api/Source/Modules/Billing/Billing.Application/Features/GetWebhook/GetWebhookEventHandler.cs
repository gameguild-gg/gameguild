using Microsoft.Extensions.Logging;
using GameGuild.Modules.Billing.DTOs;
using GameGuild.CQRS;

namespace GameGuild.Modules.Billing.Features.GetWebhook;

/// <summary>
///     Handler for getting webhook events
/// </summary>
public class GetWebhookEventHandler : IQueryHandler<GetWebhookEventQuery, BillingWebhookEventDto?>
{
    private readonly ILogger<GetWebhookEventHandler> _logger;

    public GetWebhookEventHandler(ILogger<GetWebhookEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task<BillingWebhookEventDto?> Handle(GetWebhookEventQuery query, CancellationToken cancellationToken)
    {
        try
        {
            // TODO: Get webhook event from repository
            // var webhookEvent = await _webhookRepository.GetByIdAsync(Guid.Parse(query.EventId), cancellationToken);

            // For now, return mock result
            return await Task.FromResult(new BillingWebhookEventDto
            {
                Id = Guid.Parse(query.EventId),
                Provider = "stripe",
                ExternalEventId = "evt_test_12345",
                EventType = "payment_intent.succeeded",
                IsProcessed = true,
                CreatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting webhook event {EventId}", query.EventId);
            return null;
        }
    }
}

