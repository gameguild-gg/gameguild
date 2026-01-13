using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public record GetActiveSubscriptionPlansQuery : IQuery<IEnumerable<SubscriptionPlan>>;
