using GameGuild.CQRS;
using GameGuild.Modules.Subscriptions.Entities;


namespace GameGuild.Modules.Subscriptions.Features.GetSubscription;

public record GetSubscriptionsByPlanQuery(Guid PlanId) : IQuery<IEnumerable<Subscription>>;

