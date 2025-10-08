using MediatR;

namespace GameGuild.Modules.Subscriptions.Features.ManageSubscription;

/// <summary>
///     Command to activate a subscription
/// </summary>
public record ActivateSubscriptionCommand(Guid SubscriptionId) : ICommand;

