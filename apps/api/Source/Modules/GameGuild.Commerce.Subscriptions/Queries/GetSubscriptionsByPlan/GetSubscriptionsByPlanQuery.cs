using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public record GetSubscriptionsByPlanQuery(Guid PlanId) : IQuery<IEnumerable<Subscription>>;
