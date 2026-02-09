using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public sealed record GetActiveSubscriptionPlansQuery : IQuery<IEnumerable<SubscriptionPlan>>;
