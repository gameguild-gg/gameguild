using GameGuild.CQRS;
using GameGuild.Subscriptions.SubscriptionPlans.Entities;

namespace GameGuild.Subscriptions.Queries;

public record SuggestSubscriptionPlanUpgradesQuery(Guid CurrentPlanId, int Users, long StorageMb, long ApiCalls) : IQuery<IEnumerable<SubscriptionPlan>>;
