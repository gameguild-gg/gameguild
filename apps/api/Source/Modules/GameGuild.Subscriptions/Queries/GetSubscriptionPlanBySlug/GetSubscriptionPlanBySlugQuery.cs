using GameGuild.CQRS;
using GameGuild.Subscriptions.SubscriptionPlans.Entities;

namespace GameGuild.Subscriptions.Queries;

public record GetSubscriptionPlanBySlugQuery(string Slug) : IQuery<SubscriptionPlan?>;
