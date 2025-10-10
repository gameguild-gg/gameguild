using GameGuild.CQRS;
using GameGuild.Modules.Subscriptions.Entities;
using GameGuild.Modules.Subscriptions.SubscriptionPlans.Entities;
using GameGuild.Modules.Subscriptions.Models;

namespace GameGuild.Modules.Subscriptions.Features.GetSubscription;

public record GetSubscriptionsByPlanQuery(Guid PlanId) : IQuery<IEnumerable<Subscription>>;

