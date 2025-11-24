using GameGuild.CQRS;
using GameGuild.Subscriptions.Entities;

namespace GameGuild.Subscriptions.Queries;

public record GetSubscriptionsByPlanQuery(Guid PlanId) : IQuery<IEnumerable<Subscription>>;
