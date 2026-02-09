using GameGuild.CQRS;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Command to retry a failed webhook event
/// </summary>
public sealed record RetryWebhookEventCommand(string EventId) : ICommand<WebhookRetryResult>;
