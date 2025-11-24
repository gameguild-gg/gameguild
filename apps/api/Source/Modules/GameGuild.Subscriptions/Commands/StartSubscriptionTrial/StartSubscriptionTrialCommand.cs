using GameGuild.CQRS;

namespace GameGuild.Subscriptions.Commands;

public record StartSubscriptionTrialCommand(Guid SubscriptionId, int TrialDays = 30) : ICommand;
