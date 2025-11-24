using GameGuild.CQRS;
using GameGuild.Subscriptions.SubscriptionPlans.Entities;

namespace GameGuild.Subscriptions.Queries;

public record SearchSubscriptionPlansQuery(string SearchTerm) : IQuery<IEnumerable<SubscriptionPlan>>;
