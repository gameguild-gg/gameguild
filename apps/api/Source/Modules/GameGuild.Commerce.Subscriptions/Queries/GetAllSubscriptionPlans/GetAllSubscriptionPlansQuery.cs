using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public sealed record GetAllSubscriptionPlansQuery : IQuery<IEnumerable<SubscriptionPlan>>;
