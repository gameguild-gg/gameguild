using GameGuild.Billing.DTOs;
using GameGuild.CQRS;

namespace GameGuild.Billing.Queries;

/// <summary>
///     Query to get a webhook event by ID
/// </summary>
public record GetWebhookEventQuery(string EventId) : IQuery<BillingWebhookEventDto?>;
