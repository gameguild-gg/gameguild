using GameGuild.CQRS;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Query to get a webhook event by ID
/// </summary>
public record GetWebhookEventQuery(string EventId) : IQuery<BillingWebhookEventDto?>;
