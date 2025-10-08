using MediatR;

namespace GameGuild.Modules.Subscriptions.Features.ManageSubscription;

public record SetSubscriptionExternalIdsCommand(
    Guid SubscriptionId,
    string? StripeSubscriptionId,
    string? PayPalSubscriptionId
) : ICommand;

