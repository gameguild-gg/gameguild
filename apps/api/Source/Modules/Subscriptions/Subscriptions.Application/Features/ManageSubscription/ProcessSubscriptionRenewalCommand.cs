using MediatR;

namespace GameGuild.Modules.Subscriptions.Features.ManageSubscription;

public record ProcessSubscriptionRenewalCommand(Guid SubscriptionId) : ICommand;

