using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public sealed record GetSubscriptionsByStatusQuery(SubscriptionStatus Status) : IQuery<IEnumerable<Subscription>>;
