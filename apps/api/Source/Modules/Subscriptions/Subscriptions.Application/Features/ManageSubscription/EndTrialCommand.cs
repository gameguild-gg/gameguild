using MediatR;

namespace GameGuild.Modules.Subscriptions.Features.ManageSubscription;

public record EndTrialCommand(Guid SubscriptionId, bool ConvertToPaid = true) : ICommand;

