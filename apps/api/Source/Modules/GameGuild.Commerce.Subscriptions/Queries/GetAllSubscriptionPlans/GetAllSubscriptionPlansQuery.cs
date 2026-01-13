using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public record GetAllSubscriptionPlansQuery : IQuery<IEnumerable<SubscriptionPlan>>;
