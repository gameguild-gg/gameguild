using MediatR;
using GameGuild.Modules.Subscriptions.SubscriptionPlans.Entities;
using GameGuild.Modules.Subscriptions.SubscriptionPlans.Models;

namespace GameGuild.Modules.Subscriptions.Features.SubscriptionPlans;

public record SuggestSubscriptionPlanUpgradesQuery(
    Guid CurrentPlanId, 
    int Users, 
    long StorageMb, 
    long ApiCalls
) : IQuery<IEnumerable<SubscriptionPlan>>;

