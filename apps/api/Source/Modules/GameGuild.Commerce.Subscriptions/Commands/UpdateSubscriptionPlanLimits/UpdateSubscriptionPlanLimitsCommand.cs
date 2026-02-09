using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public sealed record UpdateSubscriptionPlanLimitsCommand(Guid Id, int? MaxUsers, long? MaxStorageMb, long? MaxApiCallsPerMonth) : ICommand;
