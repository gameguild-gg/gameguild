using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public sealed record GetSubscriptionPlanByIdQuery(Guid Id) : IQuery<SubscriptionPlan?>;
