using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Query to get subscription by external ID
/// </summary>
public record GetSubscriptionByExternalIdQuery(string ExternalId) : IQuery<Subscription?>;
