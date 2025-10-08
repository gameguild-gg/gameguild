using MediatR;

namespace GameGuild.Modules.Subscriptions.Features.ManageSubscription;

/// <summary>
///     Command to set auto-renewal preference
/// </summary>
public record SetSubscriptionAutoRenewCommand(
    Guid SubscriptionId,
    bool AutoRenew
) : ICommand;

