using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public record GetFeaturedSubscriptionPlansQuery : IQuery<IEnumerable<SubscriptionPlan>>;
