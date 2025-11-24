using GameGuild.CQRS;

namespace GameGuild.Subscriptions.Commands;

public record ActivateSubscriptionPlanCommand(Guid Id) : ICommand;
