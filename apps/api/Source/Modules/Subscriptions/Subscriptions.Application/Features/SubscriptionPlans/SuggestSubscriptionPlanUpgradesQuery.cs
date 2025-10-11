using GameGuild.CQRS;
using GameGuild.Modules.Subscriptions.SubscriptionPlans.Entities;


namespace GameGuild.Modules.Subscriptions.Features.SubscriptionPlans;

public record SuggestSubscriptionPlanUpgradesQuery(
    Guid CurrentPlanId, 
    int Users, 
    long StorageMb, 
    long ApiCalls
) : IQuery<IEnumerable<SubscriptionPlan>>;

