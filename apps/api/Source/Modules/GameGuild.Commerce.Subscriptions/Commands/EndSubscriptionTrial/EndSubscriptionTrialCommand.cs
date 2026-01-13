using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public record EndSubscriptionTrialCommand(Guid SubscriptionId, bool ConvertToPaid = true) : ICommand;
