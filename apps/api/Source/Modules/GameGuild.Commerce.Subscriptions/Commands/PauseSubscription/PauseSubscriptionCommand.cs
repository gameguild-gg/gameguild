using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command to pause subscription billing
/// </summary>
public record PauseSubscriptionCommand(Guid SubscriptionId, DateTime? PauseUntil = null, string? Reason = null) : ICommand;
