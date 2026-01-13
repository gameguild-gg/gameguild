using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Query to get subscription by ID
/// </summary>
public record GetSubscriptionByIdQuery(Guid SubscriptionId) : IQuery<Subscription?>;
