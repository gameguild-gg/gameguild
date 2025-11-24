using GameGuild.CQRS;

namespace GameGuild.Subscriptions.Commands;

public record UpdateSubscriptionPlanLimitsCommand(Guid Id, int? MaxUsers, long? MaxStorageMb, long? MaxApiCallsPerMonth) : ICommand;
