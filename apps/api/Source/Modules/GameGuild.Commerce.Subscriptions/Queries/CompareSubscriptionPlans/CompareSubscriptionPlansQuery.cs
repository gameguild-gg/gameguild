using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public record CompareSubscriptionPlansQuery(Guid PlanId, List<Guid> ComparisonPlanIds) : IQuery<object>;
