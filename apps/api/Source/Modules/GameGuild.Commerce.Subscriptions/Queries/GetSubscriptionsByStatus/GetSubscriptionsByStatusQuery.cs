using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public record GetSubscriptionsByStatusQuery(SubscriptionStatus Status) : IQuery<IEnumerable<Subscription>>;
