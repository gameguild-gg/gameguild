using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public sealed record GetSubscriptionPlansByPriceRangeQuery(decimal MinPrice, decimal MaxPrice) : IQuery<IEnumerable<SubscriptionPlan>>;
