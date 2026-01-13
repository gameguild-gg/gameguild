using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public record GetSubscriptionPlanUsageStatisticsQuery(Guid PlanId) : IQuery<object>;
