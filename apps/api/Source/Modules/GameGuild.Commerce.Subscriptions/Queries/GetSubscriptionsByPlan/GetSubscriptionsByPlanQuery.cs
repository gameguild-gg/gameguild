using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public sealed record GetSubscriptionsByPlanQuery(Guid PlanId) : IQuery<IEnumerable<Subscription>>;
