using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public sealed record GetSubscriptionPlanUsageStatisticsQuery(Guid PlanId) : IQuery<object>;
