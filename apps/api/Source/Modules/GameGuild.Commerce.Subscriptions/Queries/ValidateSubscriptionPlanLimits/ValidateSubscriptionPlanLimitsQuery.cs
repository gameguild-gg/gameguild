using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public record ValidateSubscriptionPlanLimitsQuery(Guid PlanId, int Users, long StorageMb, long ApiCalls) : IQuery<bool>;
