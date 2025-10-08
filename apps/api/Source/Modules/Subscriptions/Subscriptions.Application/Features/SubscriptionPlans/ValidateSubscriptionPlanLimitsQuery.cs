using MediatR;
using GameGuild.Modules.Subscriptions.SubscriptionPlans.Entities;

namespace GameGuild.Modules.Subscriptions.Features.SubscriptionPlans;

public record ValidateSubscriptionPlanLimitsQuery(
    Guid PlanId,
    int Users,
    long StorageMb,
    long ApiCalls
) : IQuery<bool>;

