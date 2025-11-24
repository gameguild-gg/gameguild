using GameGuild.CQRS;
using GameGuild.Subscriptions.SubscriptionPlans.Entities;

namespace GameGuild.Subscriptions.Queries;

public record GetAllSubscriptionPlansQuery : IQuery<IEnumerable<SubscriptionPlan>>;
