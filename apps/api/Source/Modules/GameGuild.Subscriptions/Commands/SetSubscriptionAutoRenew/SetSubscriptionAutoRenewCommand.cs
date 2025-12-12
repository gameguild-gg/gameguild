using GameGuild.CQRS;

namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Command to set auto-renewal preference
/// </summary>
public record SetSubscriptionAutoRenewCommand(Guid SubscriptionId, bool AutoRenew) : ICommand;
