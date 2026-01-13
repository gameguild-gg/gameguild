using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public record GetSubscriptionPlansByPriceRangeQuery(decimal MinPrice, decimal MaxPrice) : IQuery<IEnumerable<SubscriptionPlan>>;
