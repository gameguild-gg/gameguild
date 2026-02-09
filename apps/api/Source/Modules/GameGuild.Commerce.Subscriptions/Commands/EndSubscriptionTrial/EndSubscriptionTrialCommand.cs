using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public sealed record EndSubscriptionTrialCommand(Guid SubscriptionId, bool ConvertToPaid = true) : ICommand;
