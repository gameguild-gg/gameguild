using MediatR;
using GameGuild.Modules.Subscriptions.SubscriptionPlans.Entities;

namespace GameGuild.Modules.Subscriptions.Features.SubscriptionPlans;

public record GetSubscriptionPlanUsageStatisticsQuery(Guid PlanId) : IQuery<object>;

