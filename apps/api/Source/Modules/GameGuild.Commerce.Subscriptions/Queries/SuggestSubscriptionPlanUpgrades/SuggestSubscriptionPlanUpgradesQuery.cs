using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public record SuggestSubscriptionPlanUpgradesQuery(Guid CurrentPlanId, int Users, long StorageMb, long ApiCalls) : IQuery<IEnumerable<SubscriptionPlan>>;
