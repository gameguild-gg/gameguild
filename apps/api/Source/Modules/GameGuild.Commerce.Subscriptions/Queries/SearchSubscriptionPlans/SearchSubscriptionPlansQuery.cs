using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public sealed record SearchSubscriptionPlansQuery(string SearchTerm) : IQuery<IEnumerable<SubscriptionPlan>>;
