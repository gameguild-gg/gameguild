using GameGuild.CQRS;

namespace GameGuild.Modules.Subscriptions.Features.ManageSubscription;

public record StartTrialCommand(
    Guid SubscriptionId,
    int TrialDays = 30
) : ICommand;

