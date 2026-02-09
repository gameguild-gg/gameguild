using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public sealed record StartSubscriptionTrialCommand(Guid SubscriptionId, int TrialDays = 30) : ICommand;
