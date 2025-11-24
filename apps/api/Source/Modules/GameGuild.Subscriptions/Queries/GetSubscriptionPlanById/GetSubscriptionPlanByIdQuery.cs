using GameGuild.CQRS;
using GameGuild.Subscriptions.SubscriptionPlans.Entities;

namespace GameGuild.Subscriptions.Queries;

public record GetSubscriptionPlanByIdQuery(Guid Id) : IQuery<SubscriptionPlan?>;
