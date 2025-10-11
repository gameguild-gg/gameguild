using GameGuild.CQRS;


namespace GameGuild.Modules.Subscriptions.Features.SubscriptionPlans;

public record GetSubscriptionPlanUsageStatisticsQuery(Guid PlanId) : IQuery<object>;

