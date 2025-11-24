using GameGuild.CQRS;

namespace GameGuild.Subscriptions.Commands;

public record EndSubscriptionTrialCommand(Guid SubscriptionId, bool ConvertToPaid = true) : ICommand;
