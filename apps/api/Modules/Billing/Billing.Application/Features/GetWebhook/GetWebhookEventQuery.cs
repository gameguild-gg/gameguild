using GameGuild.Modules.Billing.DTOs;
using GameGuild.CQRS;

namespace GameGuild.Modules.Billing.Features.GetWebhook;

/// <summary>
///     Query to get a webhook event by ID
/// </summary>
public record GetWebhookEventQuery(
    string EventId
) : IQuery<BillingWebhookEventDto?>;

