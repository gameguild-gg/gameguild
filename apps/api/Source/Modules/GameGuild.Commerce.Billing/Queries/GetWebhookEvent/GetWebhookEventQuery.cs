using GameGuild.CQRS;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Query to get a webhook event by ID
/// </summary>
public sealed record GetWebhookEventQuery(string EventId) : IQuery<BillingWebhookEventDto?>;
