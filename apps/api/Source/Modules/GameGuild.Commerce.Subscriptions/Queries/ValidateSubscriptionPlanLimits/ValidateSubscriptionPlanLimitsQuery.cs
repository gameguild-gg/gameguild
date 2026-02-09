using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public sealed record ValidateSubscriptionPlanLimitsQuery(Guid PlanId, int Users, long StorageMb, long ApiCalls) : IQuery<bool>;
