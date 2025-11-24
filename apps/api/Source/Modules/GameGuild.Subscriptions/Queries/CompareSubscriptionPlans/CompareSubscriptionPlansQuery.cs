using GameGuild.CQRS;

namespace GameGuild.Subscriptions.Queries;

public record CompareSubscriptionPlansQuery(Guid PlanId, List<Guid> ComparisonPlanIds) : IQuery<object>;
