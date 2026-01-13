using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public record SearchSubscriptionPlansQuery(string SearchTerm) : IQuery<IEnumerable<SubscriptionPlan>>;
