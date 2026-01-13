using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public record StartSubscriptionTrialCommand(Guid SubscriptionId, int TrialDays = 30) : ICommand;
