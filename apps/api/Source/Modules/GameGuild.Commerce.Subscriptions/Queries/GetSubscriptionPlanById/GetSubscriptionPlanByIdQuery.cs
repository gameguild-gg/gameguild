using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public record GetSubscriptionPlanByIdQuery(Guid Id) : IQuery<SubscriptionPlan?>;
