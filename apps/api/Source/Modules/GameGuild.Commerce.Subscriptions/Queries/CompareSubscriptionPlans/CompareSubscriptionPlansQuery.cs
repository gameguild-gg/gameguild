using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public sealed record CompareSubscriptionPlansQuery(Guid PlanId, List<Guid> ComparisonPlanIds) : IQuery<object>;
