using GameGuild.CQRS;


namespace GameGuild.Modules.Subscriptions.Features.SubscriptionPlans;

public record CompareSubscriptionPlansQuery(
    Guid PlanId,
    List<Guid> ComparisonPlanIds
) : IQuery<object>;

