using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public sealed record GetSubscriptionPlanBySlugQuery(string Slug) : IQuery<SubscriptionPlan?>;
