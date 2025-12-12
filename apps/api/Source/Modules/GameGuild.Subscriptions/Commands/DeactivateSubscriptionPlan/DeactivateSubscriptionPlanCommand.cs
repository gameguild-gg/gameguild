using GameGuild.CQRS;

namespace GameGuild.Subscriptions.Commands;

public record DeactivateSubscriptionPlanCommand(Guid Id) : ICommand;
