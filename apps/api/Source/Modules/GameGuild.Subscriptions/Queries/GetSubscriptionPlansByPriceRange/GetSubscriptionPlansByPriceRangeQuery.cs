using GameGuild.CQRS;
using GameGuild.Subscriptions.SubscriptionPlans.Entities;

namespace GameGuild.Subscriptions.Queries;

public record GetSubscriptionPlansByPriceRangeQuery(decimal MinPrice, decimal MaxPrice) : IQuery<IEnumerable<SubscriptionPlan>>;
