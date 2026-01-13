using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public record GetSubscriptionPlanBySlugQuery(string Slug) : IQuery<SubscriptionPlan?>;
