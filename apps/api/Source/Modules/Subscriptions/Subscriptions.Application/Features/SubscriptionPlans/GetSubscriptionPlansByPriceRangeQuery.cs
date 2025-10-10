using GameGuild.CQRS;
using GameGuild.Modules.Subscriptions.SubscriptionPlans.Entities;
using GameGuild.Modules.Subscriptions.SubscriptionPlans.Models;

namespace GameGuild.Modules.Subscriptions.Features.SubscriptionPlans;

public record GetSubscriptionPlansByPriceRangeQuery(
    decimal MinPrice,
    decimal MaxPrice
) : IQuery<IEnumerable<SubscriptionPlan>>;

