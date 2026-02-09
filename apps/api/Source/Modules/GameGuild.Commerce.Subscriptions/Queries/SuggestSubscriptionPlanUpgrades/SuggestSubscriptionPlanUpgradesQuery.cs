using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public sealed record SuggestSubscriptionPlanUpgradesQuery(Guid CurrentPlanId, int Users, long StorageMb, long ApiCalls) : IQuery<IEnumerable<SubscriptionPlan>>;
