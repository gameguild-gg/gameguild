using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command to resume a paused subscription
/// </summary>
public sealed record ResumeSubscriptionCommand(Guid SubscriptionId) : ICommand;
