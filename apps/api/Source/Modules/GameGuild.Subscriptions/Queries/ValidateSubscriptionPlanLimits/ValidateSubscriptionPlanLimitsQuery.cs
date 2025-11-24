using GameGuild.CQRS;

namespace GameGuild.Subscriptions.Queries;

public record ValidateSubscriptionPlanLimitsQuery(Guid PlanId, int Users, long StorageMb, long ApiCalls) : IQuery<bool>;
