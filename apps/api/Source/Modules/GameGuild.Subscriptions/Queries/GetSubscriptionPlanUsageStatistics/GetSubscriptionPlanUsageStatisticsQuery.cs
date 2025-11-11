using GameGuild.CQRS;

namespace GameGuild.Subscriptions.Queries;

public record GetSubscriptionPlanUsageStatisticsQuery(Guid PlanId) : IQuery<object>;
